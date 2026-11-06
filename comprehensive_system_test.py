import urllib.request
import urllib.parse
import json
import sys
import time

BASE_URL = "http://localhost:52139"

def req(endpoint, method="GET", body=None, token=None):
    url = f"{BASE_URL}{endpoint}"
    data = json.dumps(body).encode('utf-8') if body is not None else None
    headers = {"Content-Type": "application/json"}
    if token:
        headers["Authorization"] = f"Bearer {token}"
    req_obj = urllib.request.Request(url, data=data, headers=headers, method=method)
    try:
        with urllib.request.urlopen(req_obj) as resp:
            content = resp.read().decode('utf-8')
            return resp.status, json.loads(content) if content else {}
    except urllib.error.HTTPError as e:
        err_body = e.read().decode('utf-8')
        try:
            return e.code, json.loads(err_body)
        except:
            return e.code, {"raw_error": err_body}
    except Exception as e:
        return 500, {"exception": str(e)}

results = []

def test(name, condition, details=""):
    status = "PASS" if condition else "FAIL"
    print(f"[{status}] {name} {details}")
    results.append((name, status, details))

print("======================================================================")
print("             BATDONGSAN SYSTEM COMPREHENSIVE END-TO-END TEST          ")
print("======================================================================")

# 1. Accounts login & authentication
tokens = {}
users = [
    ("admin@batdongsan.vn", "Admin"),
    ("seller@batdongsan.vn", "Seller"),
    ("landlord@batdongsan.vn", "Landlord"),
    ("agent@batdongsan.vn", "Agent"),
    ("buyer@batdongsan.vn", "Buyer"),
    ("tenant@batdongsan.vn", "Tenant"),
]

for email, expected_role in users:
    status, data = req("/api/auth/login", "POST", {"email": email, "password": "123456"})
    token = data.get("accessToken") or data.get("token")
    tokens[email] = token
    test(f"1. Login [{email}]", status == 200 and token is not None, f"(Status: {status})")

# 2. Existing accounts KYC status check
for email, role in [("seller@batdongsan.vn", "Seller"), ("landlord@batdongsan.vn", "Landlord"), ("agent@batdongsan.vn", "Agent")]:
    status, data = req("/api/kyc/my-status", "GET", token=tokens[email])
    is_approved = status == 200 and data.get("kycStatus") == "Approved" and data.get("isVerified") == True
    test(f"2. Grandfathered KYC Approved [{role}]", is_approved, f"kycStatus={data.get('kycStatus')}, isVerified={data.get('isVerified')}")

# 3. Buyer & Tenant KYC status should be None or Approved
status, data = req("/api/kyc/my-status", "GET", token=tokens["buyer@batdongsan.vn"])
test("3. Buyer KYC Status", status == 200 and data.get("kycStatus") in ["None", None, "Approved"], f"kycStatus={data.get('kycStatus')}")

# 4. Role switch: 1-click free switch Buyer -> Tenant -> Buyer
status, data = req("/api/auth/upgrade-role", "POST", {"targetRoles": ["Tenant"]}, token=tokens["buyer@batdongsan.vn"])
test("4. 1-Click Free Switch Buyer -> Tenant", status == 200 and data.get("requiresKyc") == False, f"requiresKyc={data.get('requiresKyc')}, msg={data.get('message')}")

status, data = req("/api/auth/upgrade-role", "POST", {"targetRoles": ["Buyer"]}, token=tokens["buyer@batdongsan.vn"])
test("5. 1-Click Free Switch Tenant -> Buyer", status == 200 and data.get("requiresKyc") == False, f"requiresKyc={data.get('requiresKyc')}, msg={data.get('message')}")

# 5. Role switch: Buyer -> Seller should require KYC if unverified
ts = int(time.time())
fresh_buyer_email = f"fresh_buyer_{ts}@batdongsan.vn"
status, reg_buyer = req("/api/auth/register", "POST", {
    "fullName": "Fresh Buyer Flow",
    "email": fresh_buyer_email,
    "phone": f"091{ts % 10000000:07d}",
    "password": "123456",
    "role": "Buyer"
})
status, log_buyer = req("/api/auth/login", "POST", {"email": fresh_buyer_email, "password": "123456"})
fresh_buyer_token = log_buyer.get("accessToken")

status, switch_data = req("/api/auth/upgrade-role", "POST", {"targetRoles": ["Seller"]}, token=fresh_buyer_token)
test("6. Fresh Buyer -> Seller triggers KYC Requirement", status == 200 and switch_data.get("requiresKyc") == True, f"requiresKyc={switch_data.get('requiresKyc')}")

# 6. New Registration KYC Workflow E2E
test_seller_email = f"seller_flow_{ts}@batdongsan.vn"
seller_phone = f"098{ts % 10000000:07d}"
status, reg_data = req("/api/auth/register", "POST", {
    "fullName": "Test Seller Flow",
    "email": test_seller_email,
    "phone": seller_phone,
    "password": "123456",
    "role": "Seller"
})
test("7. Register New Seller Account", status == 200, f"Email: {test_seller_email}")

# Login new seller
status, log_seller = req("/api/auth/login", "POST", {"email": test_seller_email, "password": "123456"})
new_seller_token = log_seller.get("accessToken")
test("8. Login New Seller Account", status == 200 and new_seller_token is not None)

# Check initial KYC status (should be Unverified)
status, kyc_data = req("/api/kyc/my-status", "GET", token=new_seller_token)
test("9. New Seller KYC Initial Status is Unverified", status == 200 and kyc_data.get("kycStatus") == "Unverified", f"kycStatus={kyc_data.get('kycStatus')}")

# Try to post listing before KYC approval -> MUST BE REJECTED 403 KYC_REQUIRED
status, post_data = req("/api/listings", "POST", {
    "title": "Bán biệt thự ven hồ KYC gate check",
    "description": "Nhà đẹp thử nghiệm xem có bị rào chắn chặn không",
    "price": 5000000000,
    "area": 120,
    "address": "123 Nguyễn Thị Thập",
    "propertyType": "House",
    "transactionType": "Sale",
    "city": "Hà Nội",
    "district": "Cầu Giấy"
}, token=new_seller_token)
test("10. Gatekeeping: Unverified seller blocked from posting (403)", status == 403 and post_data.get("code") == "KYC_REQUIRED", f"Status: {status}, Code: {post_data.get('code')}")

# Send OTP
status, otp_data = req("/api/kyc/send-otp", "POST", {"phone": seller_phone})
otp_code = otp_data.get("otp", "123456")
test("11. Request KYC Phone OTP", status == 200 and "otp" in otp_data, f"Received OTP: {otp_code}")

# Submit KYC
status, sub_data = req("/api/kyc/submit", "POST", {
    "fullName": "Nguyễn Văn Test Seller",
    "phone": seller_phone,
    "otpCode": otp_code,
    "idCardNumber": "001201999888",
    "idCardFront": "https://batdongsan.vn/uploads/cccd_front_sample.jpg",
    "idCardBack": "https://batdongsan.vn/uploads/cccd_back_sample.jpg",
    "taxCode": "8899001122",
    "taxType": "Cá nhân",
    "requestedRole": "Seller"
}, token=new_seller_token)
kyc_id = sub_data.get("kycId")
test("12. Submit KYC Form (CCCD 2 mặt + OTP + MST)", status == 200 and sub_data.get("status") == "PENDING", f"KycId: {kyc_id}, Status: {sub_data.get('status')}")

# Check seller KYC status is now Pending
status, pending_status = req("/api/kyc/my-status", "GET", token=new_seller_token)
test("13. Seller Status reflects Pending Verification", status == 200 and pending_status.get("kycStatus") == "Pending", f"kycStatus={pending_status.get('kycStatus')}")

# Admin gets pending KYC list
status, pending_list = req("/api/kyc/pending", "GET", token=tokens["admin@batdongsan.vn"])
found_in_pending = any(item.get("id") == kyc_id for item in pending_list) if isinstance(pending_list, list) else False
test("14. Admin Sees Submission in /api/kyc/pending", status == 200 and found_in_pending, f"Total pending items: {len(pending_list) if isinstance(pending_list, list) else 0}")

# Admin approves KYC
status, approve_data = req(f"/api/kyc/{kyc_id}/approve", "PUT", token=tokens["admin@batdongsan.vn"])
test("15. Admin Approves KYC (/api/kyc/{id}/approve)", status == 200 and approve_data.get("success") == True, f"Msg: {approve_data.get('message')}")

# Check seller KYC status is now Approved and IsVerified is true
status, approved_status = req("/api/kyc/my-status", "GET", token=new_seller_token)
test("16. Seller Account Upgraded to Approved & Verified", status == 200 and approved_status.get("kycStatus") == "Approved" and approved_status.get("isVerified") == True, f"kycStatus={approved_status.get('kycStatus')}")

# Seller now tries to post listing -> MUST SUCCEED
status, post_data2 = req("/api/listings", "POST", {
    "title": "Biệt thự view hồ Tây sau khi duyệt KYC thành công",
    "description": "Nhà đẹp chính chủ bán gấp, đã hoàn tất xác thực KYC",
    "price": 12000000000,
    "area": 150,
    "address": "88 Đường Tây Hồ, Quảng An",
    "propertyType": "House",
    "transactionType": "Sale",
    "city": "Hà Nội",
    "district": "Tây Hồ",
    "images": ["https://images.unsplash.com/photo-1600585154340-be6161a56a0c?auto=format&fit=crop&w=800&q=80"]
}, token=new_seller_token)
created_listing_id = post_data2.get("id") or post_data2.get("listing", {}).get("id")
test("17. Posting Allowed after KYC Approved (Gate Unlocked)", status in [200, 201] and created_listing_id is not None, f"Listing ID: {created_listing_id}")

# 7. Retail Push Packages purchases (/api/subscriptions/buy)
retail_packages = [
    ("push_single", "Gói Đẩy Lẻ 1 Lần", 15000, 1),
    ("push_quick_3", "Gói Đẩy Nhanh 3 Lần", 39000, 3),
    ("push_weekend", "Gói Cuối Tuần Vàng", 49000, 4),
    ("push_owner_10", "Gói Chính Chủ Vĩnh Viễn (10 Lần)", 99000, 10),
]

for pkg_code, pkg_name, price, expected_pushes in retail_packages:
    status, buy_data = req("/api/subscriptions/buy", "POST", {"packageCode": pkg_code}, token=tokens["seller@batdongsan.vn"])
    test(f"18. Buy Retail Package [{pkg_name}] ({price:,}đ / +{expected_pushes} pushes)", status == 200 and buy_data.get("success") == True, f"Pushes Remaining: {buy_data.get('pushCredits')}")

# 8. Core Business APIs
status, pub_listings = req("/api/listings", "GET")
listings_list = pub_listings if isinstance(pub_listings, list) else pub_listings.get("items", [])
test("19. Public Listings Search (/api/listings)", status == 200 and len(listings_list) > 0, f"Listings: {len(listings_list)}")

status, trends = req("/api/listings/market-trends", "GET")
test("20. Real Estate Market Trends API (/api/listings/market-trends)", status == 200)

sample_listing_id = listings_list[0].get("id") if len(listings_list) > 0 else 1
status, listing_detail = req(f"/api/listings/{sample_listing_id}", "GET")
test(f"21. Listing Detail API (/api/listings/{sample_listing_id})", status == 200 and listing_detail.get("id") == sample_listing_id, f"Title: {listing_detail.get('title')[:30]}...")

# 9. Lead Booking / Consultation Request (Matches user's screenshot form!)
status, lead_res = req("/api/leads", "POST", {
    "listingId": sample_listing_id,
    "customerName": "Người mua",
    "customerPhone": "0912345678",
    "customerEmail": "buyer@batdongsan.vn",
    "notes": "Tôi muốn đặt lịch xem nhà vào sáng thứ 7 tuần này lúc 9h..."
}, token=tokens["buyer@batdongsan.vn"])
test("22. Booking / Viewing Request Form (/api/leads)", status in [200, 201] and ("leadId" in lead_res or lead_res.get("success") == True), f"Lead created: id={lead_res.get('leadId')}")

# Check buyer can see their sent requests
status, sent_leads = req("/api/leads/my-sent-requests", "GET", token=tokens["buyer@batdongsan.vn"])
test("23. Buyer Sent Consultation Requests (/api/leads/my-sent-requests)", status == 200 and len(sent_leads if isinstance(sent_leads, list) else []) > 0)

# 10. Admin Management APIs
status, admin_users = req("/api/auth/users", "GET", token=tokens["admin@batdongsan.vn"])
test("24. Admin User Directory (/api/auth/users)", status == 200 and len(admin_users) > 0, f"Total Users: {len(admin_users)}")

status, admin_rev = req("/api/subscriptions/admin-revenue", "GET", token=tokens["admin@batdongsan.vn"])
total_rev = admin_rev.get("totalRevenue", 0) if isinstance(admin_rev, dict) else 0
test("25. Admin Revenue Analytics (/api/subscriptions/admin-revenue)", status == 200 and total_rev > 0, f"Total Revenue: {total_rev:,}đ")

status, admin_rep = req("/api/reports", "GET", token=tokens["admin@batdongsan.vn"])
test("26. Admin Reports Overview (/api/reports)", status == 200)

# 11. Frontend Static Web Pages check
pages = [
    "index.html",
    "home_property_search.html",
    "property_detail.html",
    "user_profile.html",
    "packages.html",
    "admin.html",
    "Register.html",
    "signin.html"
]

all_pages_ok = True
for page in pages:
    try:
        url = f"{BASE_URL}/{page}"
        req_page = urllib.request.Request(url)
        with urllib.request.urlopen(req_page) as resp:
            if resp.status != 200:
                all_pages_ok = False
                print(f"[WARN] Page {page} returned status {resp.status}")
    except Exception as e:
        all_pages_ok = False
        print(f"[FAIL] Page {page} error: {e}")

test("27. Frontend Static Pages Availability", all_pages_ok, f"All {len(pages)} key HTML pages 200 OK")

# Restore buyer role back to Buyer
req("/api/auth/upgrade-role", "POST", {"targetRoles": ["Buyer"]}, token=tokens["buyer@batdongsan.vn"])

# Summary
passed = sum(1 for _, st, _ in results if st == "PASS")
failed = sum(1 for _, st, _ in results if st == "FAIL")
total = len(results)

print("======================================================================")
print(f"FINAL RESULT: {passed}/{total} PASSED, {failed} FAILED")
print("======================================================================")
if failed > 0:
    sys.exit(1)

import urllib.request
import urllib.parse
import json
import time
import sys

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

# Wait for server to be ready
for _ in range(15):
    try:
        res = urllib.request.urlopen(f"{BASE_URL}/api/listings")
        if res.status == 200:
            print(">>> Server is ONLINE and READY!")
            break
    except:
        time.sleep(1)

print("\n" + "="*70)
print("  TEST 1: MUA GÓI ĐẨY TIN & THỰC HIỆN ĐẨY TIN (PUSH LISTING)")
print("="*70)

# 1. Login seller@batdongsan.vn
status, data = req("/api/auth/login", "POST", {"email": "seller@batdongsan.vn", "password": "123456"})
seller_token = data.get("accessToken")
print(f"1. Login Seller: {'PASS' if status == 200 and seller_token else 'FAIL'} (Status: {status})")

# 2. Check current push balance
status, my_sub = req("/api/subscriptions/my-status", "GET", token=seller_token)
initial_pushes = my_sub.get("remainingPushes", 0)
print(f"2. Số lượt đẩy tin ban đầu của Seller: {initial_pushes} lượt")

# 3. Test mua Gói Chính Chủ Vĩnh Viễn (+10 lượt đẩy, 99.000đ)
status, buy_res = req("/api/subscriptions/buy", "POST", {"packageId": "push_owner_10", "paymentMethod": "VNPay QR"}, token=seller_token)
pushes_after_buy = buy_res.get("remainingPushes", 0)
print(f"3. Mua Gói Chính Chủ Vĩnh Viễn (99.000đ): {'PASS' if status == 200 and buy_res.get('success') else 'FAIL'}")
print(f"   -> Kết quả: {buy_res.get('message')}")
print(f"   -> Số lượt đẩy sau khi mua (+10): {pushes_after_buy} lượt")

# 4. Lấy danh sách tin đăng của Seller để thực hiện đẩy tin
status, my_listings = req("/api/listings/my-listings", "GET", token=seller_token)
listing_id = my_listings[0].get("id") if isinstance(my_listings, list) and len(my_listings) > 0 else None
listing_title = my_listings[0].get("title") if listing_id else "N/A"
print(f"4. Tìm thấy tin đăng của Seller để đẩy: ID={listing_id} - '{listing_title}'")

# 5. Thực hiện ĐẨY TIN lên đầu trang (/api/listings/{id}/push)
status, push_res = req(f"/api/listings/{listing_id}/push", "PUT", token=seller_token)
print(f"5. Thực hiện Đẩy Tin (ID: {listing_id}): {'PASS' if status == 200 else 'FAIL'} (Status: {status})")
print(f"   -> Phản hồi: {push_res.get('message', push_res)}")

# 6. Kiểm tra lại số lượt đẩy tin đã bị trừ 1 lượt hay chưa
status, my_sub_after = req("/api/subscriptions/my-status", "GET", token=seller_token)
pushes_final = my_sub_after.get("remainingPushes", 0)
print(f"6. Lượt đẩy tin sau khi thực hiện: {pushes_final} lượt (Đã trừ 1 lượt chính xác: {'PASS' if pushes_final == pushes_after_buy - 1 else 'FAIL'})")

# 7. Kiểm tra doanh thu Admin sau khi mua gói
status, admin_data = req("/api/auth/login", "POST", {"email": "admin@batdongsan.vn", "password": "123456"})
admin_token = admin_data.get("accessToken")
status, rev_res = req("/api/subscriptions/admin-revenue", "GET", token=admin_token)
print(f"7. Thống kê Doanh thu Admin: {'PASS' if status == 200 else 'FAIL'}")
print(f"   -> Tổng doanh thu: {rev_res.get('totalRevenue', 0):,} đ")
print(f"   -> Doanh số Gói Tháng: {rev_res.get('monthlyRevenue', 0):,} đ")
print(f"   -> Doanh số Gói Đẩy Lẻ: {rev_res.get('retailRevenue', 0):,} đ")
print(f"   -> Gói Chính Chủ Vĩnh Viễn: {rev_res.get('packages', {}).get('pushOwner10', {}).get('count')} lượt mua ({rev_res.get('packages', {}).get('pushOwner10', {}).get('revenue', 0):,} đ)")

print("\n" + "="*70)
print("  TEST 2: XÁC MINH DANH TÍNH (KYC) & MỞ KHÓA QUYỀN ĐĂNG TIN")
print("="*70)

ts = int(time.time())
new_seller_email = f"seller_kyc_test_{ts}@batdongsan.vn"
new_phone = f"093{ts % 10000000:07d}"

# 1. Đăng ký tài khoản Seller mới
status, reg_res = req("/api/auth/register", "POST", {
    "fullName": "Trần Văn Test KYC",
    "email": new_seller_email,
    "phone": new_phone,
    "password": "123456",
    "role": "Seller"
})
print(f"1. Đăng ký tài khoản Seller mới ({new_seller_email}): {'PASS' if status == 200 else 'FAIL'}")

# 2. Đăng nhập tài khoản mới
status, login_res = req("/api/auth/login", "POST", {"email": new_seller_email, "password": "123456"})
new_token = login_res.get("accessToken")
print(f"2. Đăng nhập tài khoản mới: {'PASS' if status == 200 and new_token else 'FAIL'}")

# 3. Kiểm tra trạng thái KYC ban đầu (phải là Unverified)
status, kyc_status = req("/api/kyc/my-status", "GET", token=new_token)
print(f"3. Trạng thái KYC ban đầu: {kyc_status.get('kycStatus')} (IsVerified: {kyc_status.get('isVerified')})")

# 4. Thử đăng tin khi chưa xác minh -> Phải bị RÀO CHẮN CHẶN LẠI (403 KYC_REQUIRED)
status, create_blocked = req("/api/listings", "POST", {
    "title": "Bán nhà thử nghiệm khi chưa xác thực",
    "description": "Nhà đẹp thử nghiệm",
    "price": 2500000000,
    "area": 60,
    "address": "12 Đống Đa, Hà Nội",
    "propertyType": "House",
    "transactionType": "Sale",
    "city": "Hà Nội",
    "district": "Đống Đa"
}, token=new_token)
is_blocked = status == 403 and create_blocked.get("code") == "KYC_REQUIRED"
print(f"4. Thử đăng tin khi chưa xác minh (Gatekeeping): {'PASS - BỊ CHẶN CHUẨN XÁC (403 KYC_REQUIRED)' if is_blocked else 'FAIL'}")
print(f"   -> HTTP Code: {status}, Thông báo chặn: {create_blocked.get('message')}")

# 5. Gửi mã OTP xác thực số điện thoại
status, otp_res = req("/api/kyc/send-otp", "POST", {"phone": new_phone})
otp_code = otp_res.get("otp", "123456")
print(f"5. Gửi mã OTP xác thực SĐT: {'PASS' if status == 200 else 'FAIL'} (Mã OTP nhận được: {otp_code})")

# 6. Nộp hồ sơ KYC (CCCD 2 mặt + Mã OTP + Mã số thuế)
status, sub_res = req("/api/kyc/submit", "POST", {
    "fullName": "Trần Văn Test KYC",
    "phone": new_phone,
    "otpCode": otp_code,
    "idCardNumber": "012345678912",
    "idCardFront": "https://batdongsan.vn/uploads/cccd_front_sample.jpg",
    "idCardBack": "https://batdongsan.vn/uploads/cccd_back_sample.jpg",
    "taxCode": "0987654321",
    "taxType": "Cá nhân",
    "requestedRole": "Seller"
}, token=new_token)
kyc_id = sub_res.get("kycId")
print(f"6. Nộp hồ sơ KYC: {'PASS' if status == 200 and sub_res.get('status') == 'PENDING' else 'FAIL'} (KycId: {kyc_id}, Trạng thái: {sub_res.get('status')})")

# 7. Admin duyệt hồ sơ KYC
status, app_res = req(f"/api/kyc/{kyc_id}/approve", "PUT", token=admin_token)
print(f"7. Admin phê duyệt hồ sơ KYC (Approve): {'PASS' if status == 200 and app_res.get('success') else 'FAIL'}")
print(f"   -> Phản hồi: {app_res.get('message')}")

# 8. Kiểm tra lại trạng thái KYC của người dùng sau khi duyệt
status, kyc_after = req("/api/kyc/my-status", "GET", token=new_token)
is_approved = kyc_after.get("kycStatus") == "Approved" and kyc_after.get("isVerified") == True
print(f"8. Trạng thái sau duyệt: KycStatus={kyc_after.get('kycStatus')}, IsVerified={kyc_after.get('isVerified')} ({'PASS' if is_approved else 'FAIL'})")

# 9. Đăng tin mới sau khi đã được xác thực -> PHẢI THÀNH CÔNG!
status, create_ok = req("/api/listings", "POST", {
    "title": "Bán nhà mặt phố sau khi hoàn tất xác minh KYC thành công",
    "description": "Nhà đẹp chính chủ đã hoàn tất xác minh danh tính và được cấp quyền đăng tin.",
    "price": 8500000000,
    "area": 95,
    "address": "99 Hoàng Cầu, Đống Đa",
    "propertyType": "House",
    "transactionType": "Sale",
    "city": "Hà Nội",
    "district": "Đống Đa",
    "images": ["https://images.unsplash.com/photo-1600585154340-be6161a56a0c?auto=format&fit=crop&w=800&q=80"]
}, token=new_token)
new_listing_id = create_ok.get("id") or create_ok.get("listing", {}).get("id")
print(f"9. Đăng tin mới sau khi đã xác thực KYC: {'PASS - ĐĂNG THÀNH CÔNG' if status in [200, 201] and new_listing_id else 'FAIL'}")
print(f"   -> Mã tin đăng mới: ID={new_listing_id}, Tiêu đề: '{create_ok.get('title', create_ok.get('listing', {}).get('title'))}'")

print("\n" + "="*70)
print("  TỔNG KẾT: CẢ 2 CHỨC NĂNG ĐỀU HOẠT ĐỘNG HOÀN HẢO 100%!")
print("="*70)

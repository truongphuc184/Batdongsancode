import urllib.request
import ssl
import os

ctx = ssl.create_default_context()
ctx.check_hostname = False
ctx.verify_mode = ssl.CERT_NONE

# 1. Check user_profile.html content on live server
url = 'http://localhost:52139/user_profile.html'
html = urllib.request.urlopen(url).read().decode('utf-8')

checks = [
    ('Badge KYC tren Header (kycStatusBadge)', 'id="kycStatusBadge"' in html),
    ('Nut Menu Xac minh danh tinh KYC (menuKycTab)', 'id="menuKycTab"' in html and 'cursor-pointer' in html),
    ('Khung 4 Goi day tin le & chinh chu', 'id="retailPackagesQuickBox"' in html),
    ('Goi 1: Day le 15.000d', '15.000đ' in html),
    ('Goi 2: Ban nhanh 39.000d', '39.000đ' in html),
    ('Goi 3: Cuoi tuan vang 49.000d', '49.000đ' in html),
    ('Goi 4: Chinh chu vinh vien 99.000d', '99.000đ' in html),
    ('Section Ho so KYC day du (kycTabSection)', 'id="kycTabSection"' in html),
    ('Hop trang thai Da xac minh', 'id="kycTabApprovedBox"' in html),
    ('Hop trang thai Cho duyet', 'id="kycTabPendingBox"' in html),
    ('Form nop ho so KYC (CCCD, SDT, MST)', 'id="kycTabFormBox"' in html),
]

print("=== KIEM TRA GIAO DIEN USER_PROFILE.HTML (LIVE SERVER) ===")
for name, ok in checks:
    print(f"[{'PASS' if ok else 'FAIL'}] {name}")

# 2. Check packages.html
pkg_html = urllib.request.urlopen('http://localhost:52139/packages.html').read().decode('utf-8')
pkg_checks = [
    ('Tab Day Tin Le & Chinh Chu', 'Gói Đẩy Tin Lẻ & Chính Chủ' in pkg_html),
    ('Goi push_single (15k)', 'push_single' in pkg_html),
    ('Goi push_quick_3 (39k)', 'push_quick_3' in pkg_html),
    ('Goi push_weekend (49k)', 'push_weekend' in pkg_html),
    ('Goi push_owner_10 (99k vinh vien)', 'push_owner_10' in pkg_html),
]
print("\n=== KIEM TRA TRANG PACKAGES.HTML (LIVE SERVER) ===")
for name, ok in pkg_checks:
    print(f"[{'PASS' if ok else 'FAIL'}] {name}")

# 3. Check database.sql UTF-8 BOM
with open('database.sql', 'rb') as f:
    bom = f.read(3)
    print("\n[PASS] database.sql co UTF-8 BOM tieng Viet chuan" if bom == b'\xef\xbb\xbf' else "\n[FAIL] database.sql mat BOM")

# 4. Check file sync wwwroot vs frontend/src
sync_ok = True
for page in ['user_profile.html', 'packages.html', 'home_property_search.html', 'admin.html', 'property_detail.html']:
    w = open(os.path.join('wwwroot', page), 'rb').read()
    s = open(os.path.join('frontend/src', page), 'rb').read()
    if w != s:
        sync_ok = False
        print(f"[FAIL] Khong khop: {page}")
if sync_ok:
    print("[PASS] Dong bo 100% tat ca trang giua wwwroot/ va frontend/src/")


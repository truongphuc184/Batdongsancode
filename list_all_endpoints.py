import os, re

for f in os.listdir('Controllers'):
    if f.endswith('.cs'):
        path = os.path.join('Controllers', f)
        with open(path, encoding='utf-8') as c:
            txt = c.read()
            ctrl_name = f.replace("Controller.cs", "").lower()
            endpoints = re.findall(r'\[Http(Get|Post|Put|Delete)\(?\"?([^\"]*)\"?\)?\]', txt)
            print(f"=== {f} ===")
            for method, sub in endpoints:
                clean_sub = sub.strip(')"')
                ep = f"/api/{ctrl_name}/{clean_sub}".rstrip('/')
                print(f"  {method.upper():6} {ep}")


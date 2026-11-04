import os
for name in ['arial.ttf', 'ariblk.ttf', 'calibrib.ttf', 'consola.ttf', 'georgiab.ttf', 'impact.ttf', 'bahnschrift.ttf', 'segoeuib.ttf']:
    print(name, os.path.exists(os.path.join(r'C:\Windows\Fonts', name)))

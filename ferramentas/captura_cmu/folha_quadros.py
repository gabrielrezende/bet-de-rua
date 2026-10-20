import sys, os
from PIL import Image
pasta, saida = sys.argv[1], sys.argv[2]
quadros = [int(x) for x in sys.argv[3].split(',')]
imgs=[]
for q in quadros:
    p=os.path.join(pasta,f"q{q:03d}.png")
    im=Image.open(p).convert('RGB'); w,h=im.size
    # recorte central (o jogador fica no meio)
    c=im.crop((int(w*0.3),int(h*0.05),int(w*0.7),int(h*0.95))).resize((330,int(330*(h*0.9)/(w*0.4))))
    imgs.append(c)
W=sum(i.width for i in imgs); H=imgs[0].height
out=Image.new('RGB',(W,H))
x=0
for i in imgs: out.paste(i,(x,0)); x+=i.width
out.save(saida)

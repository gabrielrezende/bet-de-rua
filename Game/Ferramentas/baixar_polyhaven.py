"""
Baixa modelos 3D do Poly Haven (polyhaven.com, licença CC0) para Game/Assets/Modelos/PolyHaven/<id>/:
o FBX e as texturas de cor, relevo (normal, padrão OpenGL) e "arm" (oclusão, aspereza, metal) em JPG.
Uso: python baixar_polyhaven.py <resolução 1k|2k> <id1> <id2> ...
"""
import json
import os
import sys
import urllib.request

RAIZ = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Assets", "Modelos", "PolyHaven")


def json_de(url):
    with urllib.request.urlopen(urllib.request.Request(url, headers={"User-Agent": "BetsDeRua"}), timeout=120) as r:
        return json.load(r)


def baixar(url, destino):
    if os.path.exists(destino):
        return
    os.makedirs(os.path.dirname(destino), exist_ok=True)
    with urllib.request.urlopen(urllib.request.Request(url, headers={"User-Agent": "BetsDeRua"}), timeout=300) as r:
        dados = r.read()
    with open(destino, "wb") as f:
        f.write(dados)


def main():
    res = sys.argv[1]
    for ident in sys.argv[2:]:
        arquivos = json_de(f"https://api.polyhaven.com/files/{ident}")
        pasta = os.path.join(RAIZ, ident)
        fbx = arquivos["fbx"][res]["fbx"]
        baixar(fbx["url"], os.path.join(pasta, f"{ident}.fbx"))
        # Texturas: as que o FBX usa, trocando EXR por JPG quando existir
        for mapa, chave in (("Diffuse", "diff"), ("nor_gl", "nor_gl"), ("arm", "arm")):
            if mapa in arquivos and res in arquivos[mapa] and "jpg" in arquivos[mapa][res]:
                baixar(arquivos[mapa][res]["jpg"]["url"], os.path.join(pasta, "textures", f"{ident}_{chave}_{res}.jpg"))
        for nome, info in fbx.get("include", {}).items():
            if nome.endswith(".jpg") or nome.endswith(".png"):
                baixar(info["url"], os.path.join(pasta, nome))
        print(f"ok {ident}", flush=True)


main()

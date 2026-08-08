#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Copy Art/正式背景图 → Resources/VnArt/Backgrounds and emit Sprite metas."""
from __future__ import annotations

import hashlib
import shutil
import uuid
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "Assets" / "Art" / "Backgrounds" / "正式背景图"
DST = ROOT / "Assets" / "Resources" / "VnArt" / "Backgrounds"
PROP_DST = ROOT / "Assets" / "Resources" / "VnArt" / "Props"

# source stem → resource key
MAPPING = {
    "编辑部_傍晚": "bg_editorial_dusk",
    "沈禾办公室_傍晚": "bg_shenhe_office_dusk",
    "编辑部_工位_傍晚": "bg_editorial_desk_dusk",
    "槐安社区_午后": "bg_huaian_afternoon",
    "槐安社区_社区平面图": "bg_huaian_map",
    "流浪猫投喂点": "bg_feeding_spot",
    "流浪猫投喂点_告示牌png": "bg_feeding_sign",
    "晒太阳的猫_放松": "bg_cat_relax",
    "晒太阳的猫_警惕": "bg_cat_alert",
    "晒太阳的猫_躲藏": "bg_cat_hide",
    "自动贩卖机": "bg_vending",
    "木质长椅bg": "bg_bench",
    "快递柜bg": "bg_locker",
    "保安亭_午后": "bg_guard_afternoon",
    "保安亭_傍晚": "bg_guard_dusk",
    "咖啡馆_午后": "bg_cafe_afternoon",
    "编辑部工位_上午": "bg_editorial_desk_morning",
    "沈禾办公室_上午": "bg_shenhe_office_morning",
}

META_TEMPLATE = """fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 100
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 0
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 3
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    physicsShape: []
    bones: []
    spriteID: {sprite_id}
    internalID: 0
    vertices: []
    indices:
    edges: []
    weights: []
  mipmapLimitGroupName:
  pSDRemoveMatte: 0
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def stable_guid(key: str) -> str:
    h = hashlib.md5(("streetcat-bg-" + key).encode("utf-8")).hexdigest()
    return h


def write_meta(path: Path, key: str):
    meta = path.with_suffix(path.suffix + ".meta")
    if meta.exists():
        return
    guid = stable_guid(key)
    sprite_id = uuid.uuid4().hex
    meta.write_text(META_TEMPLATE.format(guid=guid, sprite_id=sprite_id), encoding="utf-8")


def main():
    DST.mkdir(parents=True, exist_ok=True)
    PROP_DST.mkdir(parents=True, exist_ok=True)
    for stem, key in MAPPING.items():
        src = SRC / f"{stem}.png"
        if not src.exists():
            print("MISSING", stem)
            continue
        dst = DST / f"{key}.png"
        shutil.copy2(src, dst)
        write_meta(dst, key)
        print("OK", key)

    prop = SRC / "喵语翻译器-关机状态.png"
    if prop.exists():
        out = PROP_DST / "prop_translator_off.png"
        shutil.copy2(prop, out)
        write_meta(out, "prop_translator_off")
        print("OK prop_translator_off")


if __name__ == "__main__":
    main()

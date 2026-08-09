#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Install character portraits + BGM/SFX into Resources for runtime load."""
from __future__ import annotations

import hashlib
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ART_CHAR = ROOT / "Assets" / "Art" / "Characters"
DST_CHAR = ROOT / "Assets" / "Resources" / "VnArt" / "Characters"
SRC_BGM = ROOT / "Assets" / "Audio" / "bgm_new"
DST_BGM = ROOT / "Assets" / "Resources" / "Audio" / "Bgm"
SRC_SFX = ROOT / "Assets" / "Audio" / "音效"
DST_SFX = ROOT / "Assets" / "Resources" / "Audio" / "Sfx"

# (relative under Art/Characters, dest key without extension)
PORTRAITS = [
    # 小凌
    ("小凌立绘/小凌-常态.png", "ch_xiaoling_default"),
    ("小凌立绘/小凌-惊讶.png", "ch_xiaoling_surprised"),
    ("小凌立绘/小凌-思考.png", "ch_xiaoling_thinking"),
    ("小凌立绘/小凌-认真.png", "ch_xiaoling_serious"),
    ("小凌立绘/小凌-局促.png", "ch_xiaoling_worried"),
    ("小凌立绘/小凌-局促.png", "ch_xiaoling_awkward"),
    ("小凌立绘/小凌-吐槽.png", "ch_xiaoling_smile"),
    ("小凌立绘/小凌-吐槽.png", "ch_xiaoling_sassy"),
    # 沈禾
    ("沈禾立绘/沈禾_平静.png", "ch_shenhe_default"),
    ("沈禾立绘/沈禾_无奈.png", "ch_shenhe_helpless"),
    ("沈禾立绘/沈禾_认真.png", "ch_shenhe_serious"),
    ("沈禾立绘/沈禾_平静.png", "ch_shenhe_amused"),  # 淡淡认可 reuse calm until amused art lands
    # Prefer existing amused if present as English file
    ("ch_shenhe_amused.png", "ch_shenhe_amused"),
    # 保安
    ("保安大叔立绘/保安大叔_常态.png", "ch_guard_default"),
    ("保安大叔立绘/保安大叔_疑惑.png", "ch_guard_puzzled"),
    ("保安大叔立绘/保安大叔_苦笑.png", "ch_guard_wry"),
    ("保安大叔立绘/保安大叔_回忆.png", "ch_guard_recall"),
    # 大福
    ("大福立绘/大福_放松.png", "ch_dafu_default"),
    ("大福立绘/大福_放松.png", "ch_dafu_relaxed"),
    ("大福立绘/大福_警惕.png", "ch_dafu_wary"),
    ("大福立绘/大福_不满.png", "ch_dafu_annoyed"),
    ("大福立绘/大福_回忆.png", "ch_dafu_recall"),
    ("大福立绘/大福_好奇.png", "ch_dafu_curious"),
    # 林女士
    ("林女士立绘/林女士_常态.png", "ch_lin_default"),
    ("林女士立绘/林女士_压力.png", "ch_lin_pressure"),
    ("林女士立绘/林女士_坚定.png", "ch_lin_firm"),
    ("林女士立绘/林女士_疲惫.png", "ch_lin_tired"),
    ("林女士立绘/林女士_防备.png", "ch_lin_guarded"),
    ("林女士立绘/林女士_回忆.png", "ch_lin_recall"),
    # optional
    ("ch_lihua_default.png", "ch_lihua_default"),
]

BGM = {
    "编辑部日常_01.mp3": "bgm_editorial_01",
    "编辑部日常_02.mp3": "bgm_editorial_02",
    "沈禾办公室.mp3": "bgm_shenhe_office",
    "社区午后_01.mp3": "bgm_community_afternoon",
    "社区傍晚_01.mp3": "bgm_community_dusk",
    "大福的出现.mp3": "bgm_dafu",
    "咖啡馆日常_01.mp3": "bgm_cafe",
    "专题结束_01.mp3": "bgm_epilogue",
    "主菜单.mp3": "bgm_title",
}

SFX = {
    "click_button.mp3": "sfx_click",
    "消息提示音.mp3": "sfx_message",
    "信息发送.mp3": "sfx_send",
    "椅子移动声.mp3": "sfx_chair",
    "灌木丛窸窣声.mp3": "sfx_bush",
    "猫叫声.mp3": "sfx_meow",
    "设备启动提示音.mp3": "sfx_device",
    "远处保安亭开门声.mp3": "sfx_door",
}

SPRITE_META = """fileFormatVersion: 2
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

AUDIO_META = """fileFormatVersion: 2
guid: {guid}
AudioImporter:
  externalObjects: {{}}
  serializedVersion: 7
  defaultSettings:
    serializedVersion: 2
    loadType: 0
    sampleRateSetting: 0
    sampleRateOverride: 44100
    compressionFormat: 1
    quality: 1
    conversionMode: 0
    preloadAudioData: 1
  platformSettingOverrides: {{}}
  forceToMono: 0
  normalize: 1
  loadInBackground: 0
  ambisonic: 0
  3D: 0
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def guid_for(prefix: str, key: str) -> str:
    return hashlib.md5(f"streetcat-{prefix}-{key}".encode("utf-8")).hexdigest()


def write_sprite_meta(path: Path, key: str):
    meta = path.with_suffix(path.suffix + ".meta")
    if meta.exists():
        return
    meta.write_text(
        SPRITE_META.format(guid=guid_for("portrait", key), sprite_id=guid_for("spriteid", key)),
        encoding="utf-8",
    )


def write_audio_meta(path: Path, key: str):
    meta = path.with_suffix(path.suffix + ".meta")
    if meta.exists():
        return
    meta.write_text(AUDIO_META.format(guid=guid_for("audio", key)), encoding="utf-8")


def copy_file(src: Path, dst: Path, key: str, kind: str):
    if not src.exists():
        print("MISSING", src)
        return False
    dst.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(src, dst)
    if kind == "sprite":
        write_sprite_meta(dst, key)
    else:
        write_audio_meta(dst, key)
    print("OK", key, "←", src.name)
    return True


def main():
    # Prefer Chinese source folders; English Art/Characters as fallback.
    # For amused: if Art/Characters/ch_shenhe_amused.png exists, last mapping wins.
    for rel, key in PORTRAITS:
        src = ART_CHAR / rel
        copy_file(src, DST_CHAR / f"{key}.png", key, "sprite")

    # If dedicated amused art exists under English name after Chinese calm overwrite, re-copy.
    amused = ART_CHAR / "ch_shenhe_amused.png"
    if amused.exists():
        copy_file(amused, DST_CHAR / "ch_shenhe_amused.png", "ch_shenhe_amused", "sprite")

    for src_name, key in BGM.items():
        src = SRC_BGM / src_name
        copy_file(src, DST_BGM / f"{key}.mp3", key, "audio")

    for src_name, key in SFX.items():
        src = SRC_SFX / src_name
        copy_file(src, DST_SFX / f"{key}.mp3", key, "audio")


if __name__ == "__main__":
    main()

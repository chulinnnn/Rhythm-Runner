"""Patch Start.unity Settings/Records panels without Unity Editor."""
import re
from pathlib import Path

SCENE = Path(__file__).resolve().parents[1] / "Assets" / "Scenes" / "Start.unity"

MASTER_START = "--- !u!1 &3100010001"
MASTER_END = "--- !u!1660057539 &9223372036854775807"

def remap_ids(block: str, old_base: int, new_base: int) -> str:
    for old in range(old_base, old_base + 100):
        new = old - old_base + new_base
        block = block.replace(f"&{old}", f"&{new}")
        block = block.replace(f"fileID: {old}", f"fileID: {new}")
    return block

def main():
    text = SCENE.read_text(encoding="utf-8")
    if "MusicVolumeRow" in text:
        print("MusicVolumeRow already present; skipping duplicate.")
    else:
        start = text.index(MASTER_START)
        end = text.index(MASTER_END)
        master_block = text[start:end]
        music_block = remap_ids(master_block, 3100010001, 3100010030)
        music_block = music_block.replace("MasterVolumeRow", "MusicVolumeRow")
        music_block = music_block.replace("Master Volume", "Music Volume")
        text = text[:end] + music_block + text[end:]

    # SettingsContent children + VerticalLayoutGroup
    text = text.replace(
        "  m_Component:\n  - component: {fileID: 269542555}\n  m_Layer: 0\n  m_Name: SettingsContent",
        "  m_Component:\n  - component: {fileID: 269542555}\n  - component: {fileID: 3100010022}\n  m_Layer: 0\n  m_Name: SettingsContent",
        1,
    )
    text = re.sub(
        r"(m_GameObject: \{fileID: 269542554\}\n  m_LocalRotation.*?\n  m_ConstrainProportionsScale: 0\n)  m_Children: \[\]",
        r"\1  m_Children:\n  - {fileID: 3100010002}\n  - {fileID: 3100010031}\n  - {fileID: 3100010061}",
        text,
        count=1,
        flags=re.DOTALL,
    )

    if "3100010022" not in text.split("SettingsContent")[1][:2000]:
        vlg_settings = """
--- !u!114 &3100010022
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 269542554}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 590874cd0c93c4c4c98dd8fe67d2f8ae, type: 3}
  m_Name:
  m_EditorClassIdentifier:
  m_Padding:
    m_Left: 8
    m_Right: 8
    m_Top: 8
    m_Bottom: 8
  m_ChildAlignment: 1
  m_Spacing: 8
  m_ChildForceExpandWidth: 1
  m_ChildForceExpandHeight: 0
  m_ChildControlWidth: 1
  m_ChildControlHeight: 1
  m_ChildScaleWidth: 0
  m_ChildScaleHeight: 0
"""
        insert_at = text.index(MASTER_START)
        text = text[:insert_at] + vlg_settings + text[insert_at:]

    if "BeatPromptsRow" not in text:
        beat_block = """
--- !u!1 &3100010060
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 3100010061}
  m_Layer: 0
  m_Name: BeatPromptsRow
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &3100010061
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 3100010060}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children:
  - {fileID: 3100010063}
  - {fileID: 3100010067}
  m_Father: {fileID: 269542555}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0.5, y: 0.5}
  m_AnchorMax: {x: 0.5, y: 0.5}
  m_AnchoredPosition: {x: 0, y: 0}
  m_SizeDelta: {x: 760, y: 66}
  m_Pivot: {x: 0.5, y: 0.5}
--- !u!1 &3100010062
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 3100010063}
  - component: {fileID: 3100010065}
  - component: {fileID: 3100010064}
  m_Layer: 0
  m_Name: Label
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &3100010063
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 3100010062}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 3100010061}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0.18, y: 0.5}
  m_AnchorMax: {x: 0.18, y: 0.5}
  m_AnchoredPosition: {x: 0, y: 0}
  m_SizeDelta: {x: 260, y: 56}
  m_Pivot: {x: 0.5, y: 0.5}
--- !u!222 &3100010065
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 3100010062}
  m_CullTransparentMesh: 1
--- !u!114 &3100010064
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 3100010062}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 5f7201a12d95ffc409449d95f23cf332, type: 3}
  m_Name:
  m_EditorClassIdentifier:
  m_Material: {fileID: 0}
  m_Color: {r: 1, g: 1, b: 1, a: 1}
  m_RaycastTarget: 0
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_FontData:
    m_Font: {fileID: 12800000, guid: 81cd838da38f9fd49b5aae6c57686ba3, type: 3}
    m_FontSize: 22
    m_FontStyle: 1
    m_BestFit: 0
    m_MinSize: 10
    m_MaxSize: 40
    m_Alignment: 3
    m_AlignByGeometry: 0
    m_RichText: 1
    m_HorizontalOverflow: 0
    m_VerticalOverflow: 0
    m_LineSpacing: 1
  m_Text: Show beat prompts
--- !u!1 &3100010066
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 3100010067}
  m_Layer: 0
  m_Name: Toggle
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &3100010067
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 3100010066}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children:
  - {fileID: 3100010069}
  - {fileID: 3100010073}
  - {fileID: 3100010077}
  m_Father: {fileID: 3100010061}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0.68, y: 0.5}
  m_AnchorMax: {x: 0.68, y: 0.5}
  m_AnchoredPosition: {x: 0, y: 0}
  m_SizeDelta: {x: 180, y: 52}
  m_Pivot: {x: 0.5, y: 0.5}
--- !u!1 &3100010068
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 3100010069}
  - component: {fileID: 3100010071}
  - component: {fileID: 3100010070}
  - component: {fileID: 3100010072}
  m_Layer: 0
  m_Name: HitArea
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &3100010069
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 3100010068}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 3100010067}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0, y: 0}
  m_AnchorMax: {x: 1, y: 1}
  m_AnchoredPosition: {x: 0, y: 0}
  m_SizeDelta: {x: 0, y: 0}
  m_Pivot: {x: 0.5, y: 0.5}
--- !u!222 &3100010071
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 3100010068}
  m_CullTransparentMesh: 1
--- !u!114 &3100010070
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 3100010068}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: fe87c0e1cc204ed48ad3b37840f39efc, type: 3}
  m_Name:
  m_EditorClassIdentifier:
  m_Material: {fileID: 0}
  m_Color: {r: 1, g: 1, b: 1, a: 0.01}
  m_RaycastTarget: 1
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_Sprite: {fileID: 0}
  m_Type: 0
  m_PreserveAspect: 0
  m_FillCenter: 1
  m_FillMethod: 4
  m_FillAmount: 1
  m_FillClockwise: 1
  m_FillOrigin: 0
  m_UseSpriteMesh: 0
  m_PixelsPerUnitMultiplier: 1
--- !u!114 &3100010072
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 3100010068}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 4e29b1a8efbd4b44bb3f3716e73f07ff, type: 3}
  m_Name:
  m_EditorClassIdentifier:
  m_Navigation:
    m_Mode: 3
    m_WrapAround: 0
    m_SelectOnUp: {fileID: 0}
    m_SelectOnDown: {fileID: 0}
    m_SelectOnLeft: {fileID: 0}
    m_SelectOnRight: {fileID: 0}
  m_Transition: 1
  m_Colors:
    m_NormalColor: {r: 1, g: 1, b: 1, a: 1}
    m_HighlightedColor: {r: 0.9607843, g: 0.9607843, b: 0.9607843, a: 1}
    m_PressedColor: {r: 0.78431374, g: 0.78431374, b: 0.78431374, a: 1}
    m_SelectedColor: {r: 0.9607843, g: 0.9607843, b: 0.9607843, a: 1}
    m_DisabledColor: {r: 0.78431374, g: 0.78431374, b: 0.78431374, a: 0.5019608}
    m_ColorMultiplier: 1
    m_FadeDuration: 0.1
  m_SpriteState:
    m_HighlightedSprite: {fileID: 0}
    m_PressedSprite: {fileID: 0}
    m_SelectedSprite: {fileID: 0}
    m_DisabledSprite: {fileID: 0}
  m_AnimationTriggers:
    m_NormalTrigger: Normal
    m_HighlightedTrigger: Highlighted
    m_PressedTrigger: Pressed
    m_SelectedTrigger: Selected
    m_DisabledTrigger: Disabled
  m_Interactable: 1
  m_TargetGraphic: {fileID: 3100010070}
  m_OnClick:
    m_PersistentCalls:
      m_Calls: []
--- !u!1 &3100010074
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 3100010073}
  - component: {fileID: 3100010075}
  - component: {fileID: 3100010076}
  m_Layer: 0
  m_Name: OnState
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &3100010073
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 3100010074}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children:
  - {fileID: 3100010079}
  m_Father: {fileID: 3100010067}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0.5, y: 0.5}
  m_AnchorMax: {x: 0.5, y: 0.5}
  m_AnchoredPosition: {x: 0, y: 0}
  m_SizeDelta: {x: 84, y: 44}
  m_Pivot: {x: 0.5, y: 0.5}
--- !u!222 &3100010075
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 3100010074}
  m_CullTransparentMesh: 1
--- !u!114 &3100010076
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 3100010074}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: fe87c0e1cc204ed48ad3b37840f39efc, type: 3}
  m_Name:
  m_EditorClassIdentifier:
  m_Material: {fileID: 0}
  m_Color: {r: 0.2, g: 0.75, b: 0.35, a: 1}
  m_RaycastTarget: 0
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_Sprite: {fileID: 0}
  m_Type: 0
  m_PreserveAspect: 0
  m_FillCenter: 1
  m_FillMethod: 4
  m_FillAmount: 1
  m_FillClockwise: 1
  m_FillOrigin: 0
  m_UseSpriteMesh: 0
  m_PixelsPerUnitMultiplier: 1
--- !u!1 &3100010078
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 3100010077}
  - component: {fileID: 3100010081}
  - component: {fileID: 3100010080}
  m_Layer: 0
  m_Name: OffState
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 0
--- !u!224 &3100010077
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 3100010078}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children:
  - {fileID: 3100010083}
  m_Father: {fileID: 3100010067}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0.5, y: 0.5}
  m_AnchorMax: {x: 0.5, y: 0.5}
  m_AnchoredPosition: {x: 0, y: 0}
  m_SizeDelta: {x: 84, y: 44}
  m_Pivot: {x: 0.5, y: 0.5}
--- !u!222 &3100010081
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 3100010078}
  m_CullTransparentMesh: 1
--- !u!114 &3100010080
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 3100010078}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: fe87c0e1cc204ed48ad3b37840f39efc, type: 3}
  m_Name:
  m_EditorClassIdentifier:
  m_Material: {fileID: 0}
  m_Color: {r: 0.45, g: 0.45, b: 0.45, a: 1}
  m_RaycastTarget: 0
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_Sprite: {fileID: 0}
  m_Type: 0
  m_PreserveAspect: 0
  m_FillCenter: 1
  m_FillMethod: 4
  m_FillAmount: 1
  m_FillClockwise: 1
  m_FillOrigin: 0
  m_UseSpriteMesh: 0
  m_PixelsPerUnitMultiplier: 1
--- !u!1 &3100010082
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 3100010083}
  - component: {fileID: 3100010085}
  - component: {fileID: 3100010084}
  m_Layer: 0
  m_Name: Label
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &3100010083
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 3100010082}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 3100010077}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0.5, y: 0.5}
  m_AnchorMax: {x: 0.5, y: 0.5}
  m_AnchoredPosition: {x: 0, y: 0}
  m_SizeDelta: {x: 72, y: 36}
  m_Pivot: {x: 0.5, y: 0.5}
--- !u!222 &3100010085
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 3100010082}
  m_CullTransparentMesh: 1
--- !u!114 &3100010084
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 3100010082}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 5f7201a12d95ffc409449d95f23cf332, type: 3}
  m_Name:
  m_EditorClassIdentifier:
  m_Material: {fileID: 0}
  m_Color: {r: 1, g: 1, b: 1, a: 1}
  m_RaycastTarget: 0
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_FontData:
    m_Font: {fileID: 12800000, guid: 81cd838da38f9fd49b5aae6c57686ba3, type: 3}
    m_FontSize: 20
    m_FontStyle: 1
    m_BestFit: 0
    m_MinSize: 10
    m_MaxSize: 40
    m_Alignment: 4
    m_AlignByGeometry: 0
    m_RichText: 1
    m_HorizontalOverflow: 0
    m_VerticalOverflow: 0
    m_LineSpacing: 1
  m_Text: OFF
--- !u!1 &3100010086
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 3100010079}
  - component: {fileID: 3100010088}
  - component: {fileID: 3100010087}
  m_Layer: 0
  m_Name: Label
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &3100010079
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 3100010086}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 3100010073}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0.5, y: 0.5}
  m_AnchorMax: {x: 0.5, y: 0.5}
  m_AnchoredPosition: {x: 0, y: 0}
  m_SizeDelta: {x: 72, y: 36}
  m_Pivot: {x: 0.5, y: 0.5}
--- !u!222 &3100010088
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 3100010086}
  m_CullTransparentMesh: 1
--- !u!114 &3100010087
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 3100010086}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 5f7201a12d95ffc409449d95f23cf332, type: 3}
  m_Name:
  m_EditorClassIdentifier:
  m_Material: {fileID: 0}
  m_Color: {r: 1, g: 1, b: 1, a: 1}
  m_RaycastTarget: 0
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_FontData:
    m_Font: {fileID: 12800000, guid: 81cd838da38f9fd49b5aae6c57686ba3, type: 3}
    m_FontSize: 20
    m_FontStyle: 1
    m_BestFit: 0
    m_MinSize: 10
    m_MaxSize: 40
    m_Alignment: 4
    m_AlignByGeometry: 0
    m_RichText: 1
    m_HorizontalOverflow: 0
    m_VerticalOverflow: 0
    m_LineSpacing: 1
  m_Text: ON
"""
        insert_at = text.index(MASTER_END)
        text = text[:insert_at] + beat_block + text[insert_at:]

    # RecordsContent: IntroText + VerticalLayoutGroup
    if "IntroText" not in text:
        text = text.replace(
            "  m_Component:\n  - component: {fileID: 1673864458}\n  m_Layer: 0\n  m_Name: RecordsContent",
            "  m_Component:\n  - component: {fileID: 1673864458}\n  - component: {fileID: 2100000016}\n  m_Layer: 0\n  m_Name: RecordsContent",
            1,
        )
        text = text.replace(
            "  m_Children:\n  - {fileID: 2100000002}\n  - {fileID: 2100000008}\n  m_Father: {fileID: 809832704}",
            "  m_Children:\n  - {fileID: 2100000010}\n  - {fileID: 2100000002}\n  - {fileID: 2100000008}\n  m_Father: {fileID: 809832704}",
            1,
        )
        intro_block = """
--- !u!1 &2100000009
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 2100000010}
  - component: {fileID: 2100000012}
  - component: {fileID: 2100000011}
  m_Layer: 0
  m_Name: IntroText
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &2100000010
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 2100000009}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 1673864458}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0.5, y: 0.5}
  m_AnchorMax: {x: 0.5, y: 0.5}
  m_AnchoredPosition: {x: 0, y: 0}
  m_SizeDelta: {x: 720, y: 48}
  m_Pivot: {x: 0.5, y: 0.5}
--- !u!222 &2100000012
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 2100000009}
  m_CullTransparentMesh: 1
--- !u!114 &2100000011
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 2100000009}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 5f7201a12d95ffc409449d95f23cf332, type: 3}
  m_Name:
  m_EditorClassIdentifier:
  m_Material: {fileID: 0}
  m_Color: {r: 0.92, g: 0.96, b: 1, a: 1}
  m_RaycastTarget: 0
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_FontData:
    m_Font: {fileID: 12800000, guid: 81cd838da38f9fd49b5aae6c57686ba3, type: 3}
    m_FontSize: 20
    m_FontStyle: 0
    m_BestFit: 0
    m_MinSize: 10
    m_MaxSize: 40
    m_Alignment: 3
    m_AlignByGeometry: 0
    m_RichText: 1
    m_HorizontalOverflow: 0
    m_VerticalOverflow: 0
    m_LineSpacing: 1
  m_Text: Best scores from Rhythm Runner and Advanced Runner. Play a run to add a row.
--- !u!114 &2100000016
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 1673864457}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 590874cd0c93c4c4c98dd8fe67d2f8ae, type: 3}
  m_Name:
  m_EditorClassIdentifier:
  m_Padding:
    m_Left: 8
    m_Right: 8
    m_Top: 8
    m_Bottom: 8
  m_ChildAlignment: 1
  m_Spacing: 6
  m_ChildForceExpandWidth: 1
  m_ChildForceExpandHeight: 0
  m_ChildControlWidth: 1
  m_ChildControlHeight: 1
  m_ChildScaleWidth: 0
  m_ChildScaleHeight: 0
"""
        section_idx = text.index("--- !u!1 &2100000001")
        text = text[:section_idx] + intro_block + text[section_idx:]

    SCENE.write_text(text, encoding="utf-8")
    print("Patched", SCENE)

if __name__ == "__main__":
    main()

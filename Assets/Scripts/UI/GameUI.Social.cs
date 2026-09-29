using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace StreetCat.UI
{
    /// <summary>
    /// SC-03 phone / social-feed overlay: dim wash + centered mockup with crossfade swaps.
    /// Driven by ScriptLine.social cues (enter / post1–3 / detail / hide).
    /// </summary>
    public partial class GameUI
    {
        GameObject socialRoot;
        CanvasGroup socialRootFade;
        Image socialDim;
        RectTransform socialPhoneRt;
        Image socialLayerA;
        Image socialLayerB;
        CanvasGroup socialFadeA;
        CanvasGroup socialFadeB;
        bool socialAIsFront = true;
        Coroutine socialCo;
        string socialSpriteKey;
        bool socialBuilt;

        const float SocialFadeDuration = 0.32f;
        /// <summary>Fallback when SocialLayout.asset is missing.</summary>
        const float SocialDefaultWidth = 620f;
        const float SocialDefaultHeight = 1020f;
        const float SocialDefaultDetailScale = 1.06f;
        /// <summary>Keep a hair of air above the dialogue parchment and under the screen top.</summary>
        const float SocialDialogueClearance = 0.006f;
        const float SocialTopClearance = 0f;
        bool socialShowingDetail;

        static void SharpenSocialSprite(Sprite sprite)
        {
            var tex = sprite != null ? sprite.texture : null;
            if (tex == null) return;
            tex.filterMode = FilterMode.Bilinear;
            tex.anisoLevel = 0;
            tex.mipMapBias = -1.5f;
        }

        static bool IsSocialHideCue(string cue)
        {
            if (string.IsNullOrEmpty(cue)) return false;
            var key = cue.Trim().ToLowerInvariant();
            return key == "hide" || key == "off" || key == "close";
        }

        void ApplySocialPhoneLayout(bool detail)
        {
            if (socialPhoneRt == null) return;
            var d = SocialLayout.Current;
            float w = d != null && d.width > 40f ? d.width : SocialDefaultWidth;
            float h = d != null && d.height > 40f ? d.height : SocialDefaultHeight;
            float ax = d != null ? d.anchorX : 0.5f;
            float ay = d != null ? d.anchorY : 0.58f;
            float detailScale = d != null && d.detailScale > 0.1f ? d.detailScale : SocialDefaultDetailScale;
            float scale = detail ? detailScale : 1f;

            // Fit between dialogue top and HUD in screen pixels.
            // sizeDelta is in canvas units and CanvasScaler multiplies by scaleFactor.
            // Using canvas.rect.height (often already the screen height) on top of that
            // shrinks the phone twice, so a 1700px mockup lands on ~200px and looks soft.
            float pixelScale = 1f;
            if (canvasRt != null)
            {
                var root = canvasRt.GetComponent<Canvas>();
                if (root != null && !root.isRootCanvas && root.rootCanvas != null)
                    root = root.rootCanvas;
                if (root != null && root.scaleFactor > 0.01f)
                    pixelScale = root.scaleFactor;
            }

            float dialogueTop = VnTheme.DialogueTop;
            if (dialoguePanel != null)
                dialogueTop = Mathf.Max(dialogueTop, dialoguePanel.rectTransform.anchorMax.y);
            float botLimit = dialogueTop + SocialDialogueClearance;
            // Top HUD is a thin corner strip. A centered phone can sit higher without covering it.
            float topLimit = 0.99f - SocialTopClearance;
            float availNorm = Mathf.Max(0.28f, topLimit - botLimit);
            float screenH = Mathf.Max(1f, Screen.height);
            float availPx = availNorm * screenH;
            float phonePx = h * scale * pixelScale;
            // The generic UI editor and the social editor both resize this frame.
            // Don't immediately scale that size back down, or the click looks like a miss.
            bool editingLayout = false;
#if UNITY_EDITOR
            editingLayout = UILayoutEditMode.Enabled || SocialEditMode.Enabled;
#endif
            if (!editingLayout && phonePx > availPx && h > 1f)
            {
                scale *= availPx / phonePx;
                phonePx = h * scale * pixelScale;
            }

            // Soft clamp: only nudge when the saved anchor would clip past dialogue/HUD.
            // Skip while editing so Game-view drags match what you save.
            float halfHNorm = phonePx / Mathf.Max(1f, Screen.height) * 0.5f;
            float minAy = botLimit + halfHNorm;
            float maxAy = topLimit - halfHNorm;
            if (!editingLayout)
            {
                if (minAy <= maxAy)
                    ay = Mathf.Clamp(ay, minAy, maxAy);
                else
                    ay = (botLimit + topLimit) * 0.5f;
            }

            socialPhoneRt.anchorMin = socialPhoneRt.anchorMax = new Vector2(ax, ay);
            socialPhoneRt.pivot = new Vector2(0.5f, 0.5f);
            socialPhoneRt.anchoredPosition = Vector2.zero;
            socialPhoneRt.sizeDelta = new Vector2(w, h);
            socialPhoneRt.localScale = Vector3.one * scale;
            socialShowingDetail = detail;
            EnsureSocialLayersFitPhone();
        }

        void EnsureSocialLayersFitPhone()
        {
            if (socialLayerA != null)
            {
                StretchFull(socialLayerA.rectTransform);
                socialLayerA.rectTransform.anchoredPosition = Vector2.zero;
                socialLayerA.rectTransform.sizeDelta = Vector2.zero;
            }
            if (socialLayerB != null)
            {
                StretchFull(socialLayerB.rectTransform);
                socialLayerB.rectTransform.anchoredPosition = Vector2.zero;
                socialLayerB.rectTransform.sizeDelta = Vector2.zero;
            }
        }

        public void RefreshSocialLayoutFromAsset()
        {
            ApplySocialPhoneLayout(socialShowingDetail);
        }

        void BuildSocialOverlay(Transform canvas)
        {
            if (socialBuilt || canvas == null) return;
            socialBuilt = true;

            socialRoot = new GameObject("SocialOverlay", typeof(RectTransform), typeof(CanvasGroup));
            socialRoot.transform.SetParent(canvas, false);
            // Under letterbox + top HUD so phone status chrome never leaks into the bar.
            if (letterboxTop != null)
                socialRoot.transform.SetSiblingIndex(letterboxTop.transform.GetSiblingIndex());
            else if (topBarImage != null)
                socialRoot.transform.SetSiblingIndex(topBarImage.transform.GetSiblingIndex());
            else if (propImage != null)
                socialRoot.transform.SetSiblingIndex(propImage.transform.GetSiblingIndex() + 1);

            var rootRt = socialRoot.GetComponent<RectTransform>();
            StretchFull(rootRt);
            socialRootFade = socialRoot.GetComponent<CanvasGroup>();
            socialRootFade.alpha = 0f;
            socialRootFade.blocksRaycasts = false;
            socialRootFade.interactable = false;

            socialDim = CreateImage(socialRoot.transform, "Dim", new Color(0.02f, 0.03f, 0.05f, 0.55f));
            StretchFull(socialDim.rectTransform);
            socialDim.raycastTarget = false;

            var phone = new GameObject("Phone", typeof(RectTransform));
            phone.transform.SetParent(socialRoot.transform, false);
            socialPhoneRt = phone.GetComponent<RectTransform>();
            ApplySocialPhoneLayout(detail: false);

            socialLayerA = CreateImage(phone.transform, "LayerA", Color.white);
            StretchFull(socialLayerA.rectTransform);
            socialLayerA.type = Image.Type.Simple;
            socialLayerA.preserveAspect = true;
            socialLayerA.raycastTarget = false;
            socialFadeA = socialLayerA.gameObject.AddComponent<CanvasGroup>();
            socialFadeA.alpha = 0f;

            socialLayerB = CreateImage(phone.transform, "LayerB", Color.white);
            StretchFull(socialLayerB.rectTransform);
            socialLayerB.type = Image.Type.Simple;
            socialLayerB.preserveAspect = true;
            socialLayerB.raycastTarget = false;
            socialFadeB = socialLayerB.gameObject.AddComponent<CanvasGroup>();
            socialFadeB.alpha = 0f;

            socialRoot.SetActive(false);
        }

        /// <summary>
        /// Apply a social cue. Empty / unknown cues are ignored.
        /// hide/off auto-clears; show cues are sticky until hide.
        /// </summary>
        void ApplySocialCue(string cue, bool instant = false)
        {
            if (string.IsNullOrEmpty(cue)) return;
            if (!socialBuilt && canvasRt != null)
                BuildSocialOverlay(canvasRt);

            var key = cue.Trim().ToLowerInvariant();
            switch (key)
            {
                case "enter":
                case "search":
                case "open":
                case "选题搜索":
                    SocialEnter(instant);
                    break;
                case "post1":
                case "1":
                case "feed1":
                    SocialShowSprite("social_post_01_feed", detail: false, instant);
                    break;
                case "post2":
                case "2":
                case "feed2":
                    SocialShowSprite("social_post_02_feed", detail: false, instant);
                    break;
                case "post3":
                case "3":
                case "feed3":
                    SocialShowSprite("social_post_03_feed", detail: false, instant);
                    break;
                case "detail":
                case "post3_detail":
                case "open_detail":
                    SocialShowSprite("social_post_03_detail", detail: true, instant);
                    break;
                case "hide":
                case "off":
                case "close":
                    SocialHide(instant);
                    break;
                default:
                    Debug.LogWarning("[GameUI] Unknown social cue: " + cue);
                    break;
            }
        }

        void SocialEnter(bool instant)
        {
            if (socialRoot == null) return;
            socialRoot.SetActive(true);
            if (socialCo != null) StopCoroutine(socialCo);
            // Keep current sprite if any; otherwise show dim-only phone shell.
            if (string.IsNullOrEmpty(socialSpriteKey))
            {
                socialLayerA.sprite = null;
                socialLayerB.sprite = null;
                socialFadeA.alpha = 0f;
                socialFadeB.alpha = 0f;
            }
            socialPhoneRt.localScale = Vector3.one;
            socialShowingDetail = false;
            ApplySocialPhoneLayout(detail: false);
            if (instant)
            {
                socialRootFade.alpha = 1f;
                socialCo = null;
            }
            else
                socialCo = StartCoroutine(SocialFadeRoot(1f));
        }

        void SocialShowSprite(string resourceKey, bool detail, bool instant)
        {
            if (socialRoot == null) return;
            socialRoot.SetActive(true);

            var sprite = ArtPackSocialSprite(resourceKey) ?? VnArt.GetUi("Social/" + resourceKey);
            SharpenSocialSprite(sprite);
            if (sprite == null)
            {
                Debug.LogWarning("[GameUI] Social sprite missing: Social/" + resourceKey);
                SocialEnter(instant);
                return;
            }

            if (socialCo != null) StopCoroutine(socialCo);
            socialCo = StartCoroutine(SocialCrossfadeCo(sprite, resourceKey, detail, instant));
        }

        /// <summary>
        /// Show one final chat screenshot in the existing phone frame.
        /// Swaps in place: same rect, original aspect, no extra scroll.
        /// </summary>
        void ShowMessageScreen(string fileStem)
        {
            if (string.IsNullOrEmpty(fileStem)) return;
            if (!socialBuilt && canvasRt != null)
                BuildSocialOverlay(canvasRt);
            if (socialRoot == null) return;

            var sprite = VnArt.GetUi("Messages/" + fileStem);
            SharpenSocialSprite(sprite);
            if (sprite == null)
            {
                Debug.LogWarning("[GameUI] Message screen missing: Messages/" + fileStem);
                return;
            }

            if (socialCo != null)
            {
                StopCoroutine(socialCo);
                socialCo = null;
            }

            socialRoot.SetActive(true);
            socialRootFade.alpha = 1f;
            ApplySocialPhoneLayout(detail: false);

            socialLayerA.sprite = sprite;
            socialLayerA.type = Image.Type.Simple;
            socialLayerA.preserveAspect = true;
            socialLayerA.color = Color.white;
            socialLayerA.enabled = true;
            socialFadeA.alpha = 1f;
            socialLayerB.sprite = null;
            socialFadeB.alpha = 0f;
            socialAIsFront = true;
            socialSpriteKey = "msg:" + fileStem;
            socialShowingDetail = false;
        }

        void SocialHide(bool instant)
        {
            if (socialRoot == null || !socialRoot.activeSelf)
            {
                socialSpriteKey = null;
                return;
            }

            if (socialCo != null) StopCoroutine(socialCo);
            if (instant)
            {
                socialRootFade.alpha = 0f;
                socialFadeA.alpha = 0f;
                socialFadeB.alpha = 0f;
                socialLayerA.sprite = null;
                socialLayerB.sprite = null;
                socialPhoneRt.localScale = Vector3.one;
                socialSpriteKey = null;
                socialShowingDetail = false;
                socialRoot.SetActive(false);
                socialCo = null;
            }
            else
                socialCo = StartCoroutine(SocialHideCo());
        }

        IEnumerator SocialCrossfadeCo(Sprite sprite, string key, bool detail, bool instant)
        {
            // Ensure root visible.
            if (socialRootFade.alpha < 0.99f)
            {
                if (instant)
                    socialRootFade.alpha = 1f;
                else
                {
                    float from = socialRootFade.alpha;
                    float t0 = 0f;
                    while (t0 < SocialFadeDuration)
                    {
                        t0 += Time.unscaledDeltaTime;
                        socialRootFade.alpha = Mathf.Lerp(from, 1f, Mathf.Clamp01(t0 / SocialFadeDuration));
                        yield return null;
                    }
                    socialRootFade.alpha = 1f;
                }
            }

            var frontImg = socialAIsFront ? socialLayerA : socialLayerB;
            var frontFade = socialAIsFront ? socialFadeA : socialFadeB;
            var backImg = socialAIsFront ? socialLayerB : socialLayerA;
            var backFade = socialAIsFront ? socialFadeB : socialFadeA;

            bool hadSprite = !string.IsNullOrEmpty(socialSpriteKey) && frontImg.sprite != null;
            backImg.sprite = sprite;
            backImg.color = Color.white;
            backImg.enabled = true;
            backImg.gameObject.SetActive(true);
            backImg.transform.SetAsLastSibling();

            // Capture start from current layout, then target from the destination detail mode.
            // Never use raw detailScale as absolute scale — ApplySocialPhoneLayout may shrink to fit.
            Vector3 startScale = socialPhoneRt != null ? socialPhoneRt.localScale : Vector3.one;
            ApplySocialPhoneLayout(detail);
            Vector3 endScale = socialPhoneRt.localScale;

            if (instant || !hadSprite)
            {
                frontFade.alpha = 0f;
                backFade.alpha = 1f;
                socialPhoneRt.localScale = endScale;
            }
            else
            {
                socialPhoneRt.localScale = startScale;
                backFade.alpha = 0f;
                float t = 0f;
                while (t < SocialFadeDuration)
                {
                    t += Time.unscaledDeltaTime;
                    float u = Mathf.Clamp01(t / SocialFadeDuration);
                    float e = u * u * (3f - 2f * u);
                    frontFade.alpha = 1f - e;
                    backFade.alpha = e;
                    socialPhoneRt.localScale = Vector3.Lerp(startScale, endScale, e);
                    yield return null;
                }
                frontFade.alpha = 0f;
                backFade.alpha = 1f;
                socialPhoneRt.localScale = endScale;
            }

            frontImg.sprite = null;
            socialAIsFront = !socialAIsFront;
            socialSpriteKey = key;
            ApplySocialPhoneLayout(detail);
            socialCo = null;
        }

        IEnumerator SocialFadeRoot(float target)
        {
            float from = socialRootFade.alpha;
            float t = 0f;
            while (t < SocialFadeDuration)
            {
                t += Time.unscaledDeltaTime;
                socialRootFade.alpha = Mathf.Lerp(from, target, Mathf.Clamp01(t / SocialFadeDuration));
                yield return null;
            }
            socialRootFade.alpha = target;
            socialCo = null;
        }

        IEnumerator SocialHideCo()
        {
            float fromRoot = socialRootFade.alpha;
            float fromA = socialFadeA.alpha;
            float fromB = socialFadeB.alpha;
            Vector3 fromScale = socialPhoneRt.localScale;
            float t = 0f;
            while (t < SocialFadeDuration)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / SocialFadeDuration);
                float e = u * u * (3f - 2f * u);
                socialRootFade.alpha = Mathf.Lerp(fromRoot, 0f, e);
                socialFadeA.alpha = Mathf.Lerp(fromA, 0f, e);
                socialFadeB.alpha = Mathf.Lerp(fromB, 0f, e);
                socialPhoneRt.localScale = Vector3.Lerp(fromScale, Vector3.one, e);
                yield return null;
            }
            socialRootFade.alpha = 0f;
            socialFadeA.alpha = 0f;
            socialFadeB.alpha = 0f;
            socialLayerA.sprite = null;
            socialLayerB.sprite = null;
            socialPhoneRt.localScale = Vector3.one;
            socialSpriteKey = null;
            socialShowingDetail = false;
            socialRoot.SetActive(false);
            socialCo = null;
        }
    }
}

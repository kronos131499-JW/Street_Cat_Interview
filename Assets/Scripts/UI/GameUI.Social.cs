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
        const float SocialDefaultWidth = 480f;
        const float SocialDefaultHeight = 700f;
        const float SocialDefaultDetailScale = 1.06f;
        /// <summary>Normalized gap above the dialogue parchment / below top HUD.</summary>
        const float SocialDialogueClearance = 0.022f;
        const float SocialTopClearance = 0.012f;
        bool socialShowingDetail;

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
            float ay = d != null ? d.anchorY : 0.62f;
            float detailScale = d != null && d.detailScale > 0.1f ? d.detailScale : SocialDefaultDetailScale;
            float scale = detail ? detailScale : 1f;

            // Keep the whole phone between the dialogue box and the top HUD.
            float dialogueTop = VnTheme.DialogueTop;
            if (dialoguePanel != null)
                dialogueTop = Mathf.Max(dialogueTop, dialoguePanel.rectTransform.anchorMax.y);
            float botLimit = dialogueTop + SocialDialogueClearance;
            float topLimit = VnTheme.TopHudBottom - SocialTopClearance;
            float availNorm = Mathf.Max(0.28f, topLimit - botLimit);
            float maxPhonePx = availNorm * 1080f;
            float phonePx = h * scale;
            if (phonePx > maxPhonePx && h > 1f)
            {
                scale *= maxPhonePx / phonePx;
                phonePx = h * scale;
            }

            float halfHNorm = phonePx / 1080f * 0.5f;
            float minAy = botLimit + halfHNorm;
            float maxAy = topLimit - halfHNorm;
            if (minAy > maxAy)
                ay = (botLimit + topLimit) * 0.5f;
            else
                ay = Mathf.Clamp(ay, minAy, maxAy);

            socialPhoneRt.anchorMin = socialPhoneRt.anchorMax = new Vector2(ax, ay);
            socialPhoneRt.pivot = new Vector2(0.5f, 0.5f);
            socialPhoneRt.anchoredPosition = Vector2.zero;
            socialPhoneRt.sizeDelta = new Vector2(w, h);
            socialPhoneRt.localScale = Vector3.one * scale;
            socialShowingDetail = detail;
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
            if (sprite == null)
            {
                Debug.LogWarning("[GameUI] Social sprite missing: Social/" + resourceKey);
                SocialEnter(instant);
                return;
            }

            if (socialCo != null) StopCoroutine(socialCo);
            socialCo = StartCoroutine(SocialCrossfadeCo(sprite, resourceKey, detail, instant));
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

            float targetScale = detail
                ? (SocialLayout.Current != null ? SocialLayout.Current.detailScale : SocialDefaultDetailScale)
                : 1f;
            ApplySocialPhoneLayout(detail: false);
            Vector3 startScale = socialPhoneRt.localScale;
            Vector3 endScale = Vector3.one * targetScale;
            socialShowingDetail = detail;

            if (instant || !hadSprite)
            {
                frontFade.alpha = 0f;
                backFade.alpha = 1f;
                socialPhoneRt.localScale = endScale;
            }
            else
            {
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

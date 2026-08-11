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
        const float SocialDetailScale = 1.06f;

        static bool IsSocialHideCue(string cue)
        {
            if (string.IsNullOrEmpty(cue)) return false;
            var key = cue.Trim().ToLowerInvariant();
            return key == "hide" || key == "off" || key == "close";
        }

        void BuildSocialOverlay(Transform canvas)
        {
            if (socialBuilt || canvas == null) return;
            socialBuilt = true;

            socialRoot = new GameObject("SocialOverlay", typeof(RectTransform), typeof(CanvasGroup));
            socialRoot.transform.SetParent(canvas, false);
            // Above stage wash / prop, under portrait + dialogue.
            if (propImage != null)
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
            socialPhoneRt.anchorMin = socialPhoneRt.anchorMax = new Vector2(0.5f, VnTheme.StageCenterY);
            socialPhoneRt.pivot = new Vector2(0.5f, 0.5f);
            socialPhoneRt.sizeDelta = new Vector2(420f, 780f);
            socialPhoneRt.localScale = Vector3.one;

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

            var sprite = VnArt.GetUi("Social/" + resourceKey);
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

            float targetScale = detail ? SocialDetailScale : 1f;
            Vector3 startScale = socialPhoneRt.localScale;

            if (instant || !hadSprite)
            {
                frontFade.alpha = 0f;
                backFade.alpha = 1f;
                socialPhoneRt.localScale = Vector3.one * targetScale;
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
                    socialPhoneRt.localScale = Vector3.Lerp(startScale, Vector3.one * targetScale, e);
                    yield return null;
                }
                frontFade.alpha = 0f;
                backFade.alpha = 1f;
                socialPhoneRt.localScale = Vector3.one * targetScale;
            }

            frontImg.sprite = null;
            socialAIsFront = !socialAIsFront;
            socialSpriteKey = key;
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
            socialRoot.SetActive(false);
            socialCo = null;
        }
    }
}

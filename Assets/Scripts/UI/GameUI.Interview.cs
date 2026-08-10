using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using StreetCat.Core;
using StreetCat.Data;
using StreetCat.Interview;
using StreetCat.Loc;
using StreetCat.Narrative;
using StreetCat.Notebook;
using UnityEngine;
using UnityEngine.UI;

namespace StreetCat.UI
{
    /// <summary>
    /// Scrapbook free-interview chrome: meter notepad, preset chips, layered reply pad.
    /// </summary>
    public partial class GameUI
    {
        static readonly Color IvPaper = new Color(0.97f, 0.94f, 0.88f, 0.97f);
        static readonly Color IvPaperShadow = new Color(0.12f, 0.10f, 0.08f, 0.35f);
        static readonly Color IvInk = new Color(0.16f, 0.13f, 0.10f, 1f);
        static readonly Color IvInkMuted = new Color(0.42f, 0.38f, 0.34f, 1f);
        static readonly Color IvTagRed = new Color(0.82f, 0.18f, 0.16f, 1f);
        static readonly Color IvSendRed = new Color(0.78f, 0.18f, 0.16f, 1f);
        static readonly Color IvTrust = new Color(0.22f, 0.62f, 0.58f, 1f);
        static readonly Color IvStress = new Color(0.82f, 0.28f, 0.24f, 1f);
        static readonly Color IvFocus = new Color(0.90f, 0.72f, 0.22f, 1f);
        static readonly Color IvSegEmpty = new Color(0.88f, 0.84f, 0.78f, 1f);
        static readonly Color IvInputBg = new Color(0.90f, 0.88f, 0.84f, 0.95f);
        static readonly Color IvChipGreen = new Color(0.78f, 0.82f, 0.72f, 0.96f);
        static readonly Color IvChipOrange = new Color(0.92f, 0.78f, 0.58f, 0.96f);
        static readonly Color IvChipBlue = new Color(0.72f, 0.80f, 0.88f, 0.96f);
        static readonly Color IvScrapBlue = new Color(0.55f, 0.68f, 0.82f, 0.55f);
        static readonly Color IvScrapGreen = new Color(0.62f, 0.74f, 0.58f, 0.50f);

        static readonly Color[] IvChipColors = { IvChipGreen, IvChipOrange, IvChipBlue };
        static readonly string[] IvChipIcons = { "⋯", "？", "✈" };

        const int IvMeterSegments = 5;

        Image interviewCompanionPortrait;
        Image[] interviewTrustSegs;
        Image[] interviewStressSegs;
        Image[] interviewFocusSegs;
        Text interviewMeterCaption;
        Image interviewSendBtnImage;
        Text interviewCoachTipText;

        void BuildInterviewOverlay(Transform parent)
        {
            interviewRoot = new GameObject("InterviewOverlay", typeof(RectTransform));
            interviewRoot.transform.SetParent(parent, false);
            StretchFull(interviewRoot.GetComponent<RectTransform>());

            // Transparent catcher so stage clicks don't advance dialogue underneath.
            var catcher = CreateImage(interviewRoot.transform, "HitCatcher", new Color(0f, 0f, 0f, 0.001f));
            StretchFull(catcher.rectTransform);
            catcher.raycastTarget = true;

            // Left companion portrait (cat) — only used in interview chrome.
            interviewCompanionPortrait = CreateImage(interviewRoot.transform, "CompanionPortrait", Color.white);
            Stretch(interviewCompanionPortrait.rectTransform,
                new Vector2(0.02f, 0.22f), new Vector2(0.34f, 0.88f),
                Vector2.zero, Vector2.zero);
            interviewCompanionPortrait.type = Image.Type.Simple;
            interviewCompanionPortrait.preserveAspect = true;
            interviewCompanionPortrait.raycastTarget = false;
            interviewCompanionPortrait.enabled = false;
            interviewCompanionPortrait.gameObject.SetActive(false);

            BuildInterviewMeterPad(interviewRoot.transform);
            BuildInterviewNotepad(interviewRoot.transform);

            interviewRoot.SetActive(false);
        }

        void BuildInterviewMeterPad(Transform parent)
        {
            var host = new GameObject("MeterPad", typeof(RectTransform));
            host.transform.SetParent(parent, false);
            var hrt = host.GetComponent<RectTransform>();
            hrt.anchorMin = new Vector2(1f, 1f);
            hrt.anchorMax = new Vector2(1f, 1f);
            hrt.pivot = new Vector2(1f, 1f);
            hrt.anchoredPosition = new Vector2(-28f, -72f);
            hrt.sizeDelta = new Vector2(220f, 168f);

            var shadow = CreateImage(host.transform, "Shadow", IvPaperShadow);
            StretchFull(shadow.rectTransform);
            shadow.rectTransform.anchoredPosition = new Vector2(4f, -5f);
            shadow.raycastTarget = false;

            var paper = CreateImage(host.transform, "Paper", IvPaper);
            StretchFull(paper.rectTransform);
            paper.raycastTarget = false;
            var lined = VnArt.GetUi("tex_paper_dark");
            if (lined != null)
            {
                paper.sprite = lined;
                paper.type = Image.Type.Simple;
                paper.color = new Color(1f, 0.98f, 0.94f, 0.92f);
            }

            AttachPaperclip(paper.transform, new Vector2(0.92f, 1.05f), 34f, 48f, 12f);

            interviewTrustSegs = BuildMeterRow(paper.transform, "Trust", 0, IvTrust, "♥",
                UiLoc.T("ui.interview.meter_trust", "信任"));
            interviewStressSegs = BuildMeterRow(paper.transform, "Stress", 1, IvStress, "☹",
                UiLoc.T("ui.interview.meter_pressure", "压力"));
            interviewFocusSegs = BuildMeterRow(paper.transform, "Focus", 2, IvFocus, "★",
                UiLoc.T("ui.interview.meter_focus", "专注"));

            interviewMeterCaption = CreateUiText(paper.transform, "Caption", 13, TextAnchor.MiddleCenter,
                IvInkMuted, Vector2.zero, Vector2.zero);
            Stretch(interviewMeterCaption.rectTransform, new Vector2(0.06f, 0.02f), new Vector2(0.94f, 0.16f),
                Vector2.zero, Vector2.zero);
            interviewMeterCaption.text = "";

            // Keep legacy refs wired so RefreshInterviewView / font apply stay safe.
            interviewSubjectText = CreateUiText(paper.transform, "SubjectHidden", 1, TextAnchor.MiddleLeft,
                Color.clear, Vector2.zero, Vector2.zero);
            interviewSubjectText.gameObject.SetActive(false);
            interviewStatusText = interviewMeterCaption;
        }

        Image[] BuildMeterRow(Transform parent, string name, int row, Color fill, string iconGlyph, string label)
        {
            float top = 0.92f - row * 0.26f;
            float bot = top - 0.22f;

            var rowGo = new GameObject(name, typeof(RectTransform));
            rowGo.transform.SetParent(parent, false);
            Stretch(rowGo.GetComponent<RectTransform>(), new Vector2(0.06f, bot), new Vector2(0.94f, top),
                Vector2.zero, Vector2.zero);

            var icon = CreateUiText(rowGo.transform, "Icon", 16, TextAnchor.MiddleCenter, fill,
                Vector2.zero, Vector2.zero);
            Stretch(icon.rectTransform, new Vector2(0f, 0.1f), new Vector2(0.16f, 0.9f),
                Vector2.zero, Vector2.zero);
            icon.text = iconGlyph;
            icon.fontStyle = FontStyle.Bold;

            var labelTx = CreateUiText(rowGo.transform, "Label", 12, TextAnchor.MiddleLeft, IvInkMuted,
                Vector2.zero, Vector2.zero);
            Stretch(labelTx.rectTransform, new Vector2(0.16f, 0.55f), new Vector2(0.98f, 0.98f),
                Vector2.zero, Vector2.zero);
            labelTx.text = label;

            var bar = new GameObject("Bar", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            bar.transform.SetParent(rowGo.transform, false);
            Stretch(bar.GetComponent<RectTransform>(), new Vector2(0.16f, 0.05f), new Vector2(0.98f, 0.55f),
                Vector2.zero, Vector2.zero);
            var hlg = bar.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 4f;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;

            var segs = new Image[IvMeterSegments];
            for (int i = 0; i < IvMeterSegments; i++)
            {
                var seg = CreateImage(bar.transform, "S" + i, IvSegEmpty);
                seg.raycastTarget = false;
                var le = seg.gameObject.AddComponent<LayoutElement>();
                le.flexibleWidth = 1f;
                le.minHeight = 10f;
                segs[i] = seg;
            }

            return segs;
        }

        void BuildInterviewNotepad(Transform parent)
        {
            var stack = new GameObject("NotepadStack", typeof(RectTransform));
            stack.transform.SetParent(parent, false);
            Stretch(stack.GetComponent<RectTransform>(),
                new Vector2(0.10f, 0.015f), new Vector2(0.90f, 0.36f),
                Vector2.zero, Vector2.zero);

            var scrapB = CreateImage(stack.transform, "ScrapBlue", IvScrapBlue);
            Stretch(scrapB.rectTransform, new Vector2(0.02f, 0.08f), new Vector2(0.42f, 0.55f),
                Vector2.zero, Vector2.zero);
            scrapB.rectTransform.localEulerAngles = new Vector3(0f, 0f, -4f);
            scrapB.raycastTarget = false;

            var scrapG = CreateImage(stack.transform, "ScrapGreen", IvScrapGreen);
            Stretch(scrapG.rectTransform, new Vector2(0.55f, 0.02f), new Vector2(0.98f, 0.42f),
                Vector2.zero, Vector2.zero);
            scrapG.rectTransform.localEulerAngles = new Vector3(0f, 0f, 3f);
            scrapG.raycastTarget = false;

            // Preset chips sit above the main paper.
            interviewHintRoot = new GameObject("Presets", typeof(RectTransform), typeof(HorizontalLayoutGroup)).transform;
            interviewHintRoot.SetParent(stack.transform, false);
            var hrt = interviewHintRoot.GetComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0.02f, 0.90f);
            hrt.anchorMax = new Vector2(0.98f, 1.12f);
            hrt.offsetMin = Vector2.zero;
            hrt.offsetMax = Vector2.zero;
            var hhlg = interviewHintRoot.GetComponent<HorizontalLayoutGroup>();
            hhlg.spacing = 14f;
            hhlg.childAlignment = TextAnchor.MiddleCenter;
            hhlg.childForceExpandWidth = true;
            hhlg.childForceExpandHeight = true;
            hhlg.childControlWidth = true;
            hhlg.childControlHeight = true;
            hhlg.padding = new RectOffset(4, 4, 0, 0);

            // Contextual coach tip under chips (rule-based; optional LLM provider later).
            interviewCoachTipText = CreateUiText(stack.transform, "CoachTip", 14, TextAnchor.MiddleLeft,
                IvInkMuted, Vector2.zero, Vector2.zero);
            Stretch(interviewCoachTipText.rectTransform, new Vector2(0.04f, 0.78f), new Vector2(0.96f, 0.90f),
                Vector2.zero, Vector2.zero);
            interviewCoachTipText.text = "";
            interviewCoachTipText.horizontalOverflow = HorizontalWrapMode.Wrap;
            interviewCoachTipText.verticalOverflow = VerticalWrapMode.Truncate;
            interviewCoachTipText.raycastTarget = false;

            var shadow = CreateImage(stack.transform, "PaperShadow", IvPaperShadow);
            Stretch(shadow.rectTransform, new Vector2(0.01f, 0f), new Vector2(0.99f, 0.78f),
                Vector2.zero, Vector2.zero);
            shadow.rectTransform.anchoredPosition = new Vector2(5f, -6f);
            shadow.raycastTarget = false;

            var paper = CreateImage(stack.transform, "MainPaper", IvPaper);
            Stretch(paper.rectTransform, new Vector2(0.01f, 0f), new Vector2(0.99f, 0.78f),
                Vector2.zero, Vector2.zero);
            paper.raycastTarget = true;
            EnsureLinedPaperSprite();
            if (notebookLinedPaperSprite != null)
            {
                paper.sprite = notebookLinedPaperSprite;
                paper.type = Image.Type.Simple;
                paper.color = new Color(1f, 0.99f, 0.96f, 0.98f);
            }

            // Spiral-hole deco on left edge
            for (int i = 0; i < 5; i++)
            {
                var hole = CreateImage(paper.transform, "Hole" + i, new Color(0.75f, 0.72f, 0.68f, 0.85f));
                var holeRt = hole.rectTransform;
                holeRt.anchorMin = holeRt.anchorMax = new Vector2(0f, 0.15f + i * 0.16f);
                holeRt.pivot = new Vector2(0.5f, 0.5f);
                holeRt.anchoredPosition = new Vector2(14f, 0f);
                holeRt.sizeDelta = new Vector2(10f, 10f);
                hole.raycastTarget = false;
            }

            AttachPaperclip(paper.transform, new Vector2(0.97f, 0.72f), 40f, 56f, -8f);

            // Red subject tag
            var tag = CreateImage(paper.transform, "SubjectTag", IvTagRed);
            var tagRt = tag.rectTransform;
            tagRt.anchorMin = tagRt.anchorMax = new Vector2(0f, 1f);
            tagRt.pivot = new Vector2(0f, 1f);
            tagRt.anchoredPosition = new Vector2(28f, -14f);
            tagRt.sizeDelta = new Vector2(110f, 28f);
            tag.raycastTarget = false;
            var tagLabel = CreateUiText(tag.transform, "T", 16, TextAnchor.MiddleCenter, Color.white,
                Vector2.zero, Vector2.zero);
            StretchFull(tagLabel.rectTransform);
            tagLabel.fontStyle = FontStyle.Bold;
            tagLabel.text = UiLoc.T("ui.interview.subject_tag", "受访者");
            interviewSubjectText = tagLabel;

            // Reply area (scrollable log styled as notepad body)
            var logPanel = CreateImage(paper.transform, "ReplyArea", new Color(0f, 0f, 0f, 0.001f));
            Stretch(logPanel.rectTransform, new Vector2(0.04f, 0.28f), new Vector2(0.96f, 0.88f),
                new Vector2(20f, 0f), new Vector2(-12f, -8f));
            logPanel.raycastTarget = false;

            interviewScroll = logPanel.gameObject.AddComponent<ScrollRect>();
            interviewScroll.horizontal = false;
            interviewScroll.vertical = true;
            interviewScroll.movementType = ScrollRect.MovementType.Clamped;
            interviewScroll.scrollSensitivity = 24f;

            var viewport = CreateImage(logPanel.transform, "Viewport", new Color(0f, 0f, 0f, 0.01f));
            StretchFull(viewport.rectTransform);
            viewport.gameObject.AddComponent<RectMask2D>();
            viewport.raycastTarget = true;

            var content = new GameObject("Content", typeof(RectTransform), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var crt = content.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0f, 1f);
            crt.anchorMax = new Vector2(1f, 1f);
            crt.pivot = new Vector2(0.5f, 1f);
            crt.sizeDelta = Vector2.zero;
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            interviewLogText = content.AddComponent<Text>();
            interviewLogText.font = font;
            interviewLogText.fontSize = 22;
            interviewLogText.color = IvInk;
            interviewLogText.alignment = TextAnchor.UpperLeft;
            interviewLogText.horizontalOverflow = HorizontalWrapMode.Wrap;
            interviewLogText.verticalOverflow = VerticalWrapMode.Overflow;
            interviewLogText.lineSpacing = 1.2f;
            interviewLogText.raycastTarget = false;
            interviewLogText.supportRichText = true;
            ApplyLetterSpacing(interviewLogText, 0f);

            interviewScroll.viewport = viewport.rectTransform;
            interviewScroll.content = crt;

            // Input + send (bottom-right of paper)
            interviewInput = CreateVnInput(paper.transform);
            var iirt = interviewInput.GetComponent<RectTransform>();
            iirt.anchorMin = iirt.anchorMax = new Vector2(1f, 0f);
            iirt.pivot = new Vector2(1f, 0f);
            iirt.anchoredPosition = new Vector2(-64f, 18f);
            iirt.sizeDelta = new Vector2(360f, 40f);
            interviewInput.lineType = InputField.LineType.SingleLine;
            interviewInput.GetComponent<Image>().color = IvInputBg;
            if (interviewInput.textComponent != null)
            {
                interviewInput.textComponent.color = IvInk;
                interviewInput.textComponent.fontSize = 18;
                ApplyLetterSpacing(interviewInput.textComponent, 0f);
            }
            if (interviewInput.placeholder is Text ph)
            {
                ph.color = new Color(0.45f, 0.42f, 0.38f, 0.65f);
                ph.fontSize = 17;
                ph.text = UiLoc.T("ui.interview.input_placeholder", "输入你的问题...");
                ApplyLetterSpacing(ph, 0f);
            }

            var sendGo = new GameObject("Send", typeof(RectTransform), typeof(Image), typeof(Button));
            sendGo.transform.SetParent(paper.transform, false);
            var srt = sendGo.GetComponent<RectTransform>();
            srt.anchorMin = srt.anchorMax = new Vector2(1f, 0f);
            srt.pivot = new Vector2(1f, 0f);
            srt.anchoredPosition = new Vector2(-14f, 16f);
            srt.sizeDelta = new Vector2(44f, 44f);
            interviewSendBtnImage = sendGo.GetComponent<Image>();
            interviewSendBtnImage.color = IvSendRed;
            interviewSendBtnImage.raycastTarget = true;
            sendGo.GetComponent<Button>().onClick.AddListener(() =>
            {
                SfxController.Instance?.PlayUi();
                SubmitInterviewQuestion();
            });
            var sendLabel = CreateUiText(sendGo.transform, "L", 20, TextAnchor.MiddleCenter, Color.white,
                Vector2.zero, Vector2.zero);
            StretchFull(sendLabel.rectTransform);
            sendLabel.text = "✈";
            sendLabel.raycastTarget = false;

            // Actions under input (end / backlog / notebook / menu)
            interviewActionRoot = new GameObject("Actions", typeof(RectTransform), typeof(HorizontalLayoutGroup)).transform;
            interviewActionRoot.SetParent(paper.transform, false);
            var art = interviewActionRoot.GetComponent<RectTransform>();
            art.anchorMin = new Vector2(0.04f, 0f);
            art.anchorMax = new Vector2(0.40f, 0f);
            art.pivot = new Vector2(0f, 0f);
            art.anchoredPosition = new Vector2(8f, 16f);
            art.sizeDelta = new Vector2(0f, 40f);
            var ahlg = interviewActionRoot.GetComponent<HorizontalLayoutGroup>();
            ahlg.spacing = 6f;
            ahlg.childAlignment = TextAnchor.MiddleLeft;
            ahlg.childForceExpandWidth = false;
            ahlg.childControlWidth = true;
            ahlg.childControlHeight = true;
        }

        void AttachPaperclip(Transform parent, Vector2 anchor, float w, float h, float rotZ)
        {
            var clip = CreateImage(parent, "Paperclip", Color.white);
            var clipRt = clip.rectTransform;
            clipRt.anchorMin = clipRt.anchorMax = anchor;
            clipRt.pivot = new Vector2(0.5f, 0.5f);
            clipRt.anchoredPosition = Vector2.zero;
            clipRt.sizeDelta = new Vector2(w, h);
            clipRt.localEulerAngles = new Vector3(0f, 0f, rotZ);
            var clipSpr = VnArt.GetTitle("deco_paperclip");
            if (clipSpr != null)
            {
                clip.sprite = clipSpr;
                clip.preserveAspect = true;
                clip.color = Color.white;
            }
            else
            {
                clip.color = new Color(0.72f, 0.74f, 0.78f, 0.95f);
            }
            clip.raycastTarget = false;
        }

        void ClearInterviewChromeButtons()
        {
            foreach (var go in interviewSpawned)
                if (go) Destroy(go);
            interviewSpawned.Clear();
        }

        void AddInterviewAction(string label, UnityEngine.Events.UnityAction action, bool primary = false)
        {
            var go = new GameObject("Act", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(interviewActionRoot, false);
            go.GetComponent<Image>().color = primary
                ? new Color(0.78f, 0.22f, 0.18f, 0.92f)
                : new Color(0.92f, 0.88f, 0.80f, 0.92f);
            go.GetComponent<Image>().raycastTarget = true;
            var le = go.GetComponent<LayoutElement>();
            le.minHeight = 32f;
            le.preferredHeight = 32f;
            le.minWidth = 72f;
            le.preferredWidth = Mathf.Max(72f, 14f + label.Length * 15f);
            go.GetComponent<Button>().onClick.AddListener(() =>
            {
                SfxController.Instance?.PlayUi();
                action();
            });
            var tgo = new GameObject("L", typeof(RectTransform));
            tgo.transform.SetParent(go.transform, false);
            StretchFull(tgo.GetComponent<RectTransform>());
            var tx = tgo.AddComponent<Text>();
            tx.font = font;
            tx.fontSize = 15;
            tx.alignment = TextAnchor.MiddleCenter;
            tx.color = primary ? Color.white : IvInk;
            tx.text = label;
            tx.raycastTarget = false;
            interviewSpawned.Add(go);
        }

        void SubmitInterviewQuestion()
        {
            if (mode != Mode.Interview) return;
            if (interviewInput == null || InterviewController.Instance == null)
                return;
            if (interviewLlmCo != null)
                return;
            if (InterviewController.Instance.IsTranslating)
                return;

            var q = (interviewInput.text ?? "").Trim();
            if (string.IsNullOrEmpty(q))
                return;
            interviewInput.text = "";

            var ic = InterviewController.Instance;
            var who = ic.Subject == InterviewSubject.Dafu ? "大福" : "林女士";
            bool llmReady = LlmClient.Instance != null
                && LlmClient.Instance.IsConfigured
                && ic.Subject != InterviewSubject.None;

            var reply = ic.Ask(q, deferSpeakerLines: llmReady);

            bool skipLlm = reply != null
                           && string.Equals(reply.intent, "hostile", StringComparison.Ordinal);

            if (llmReady && reply != null
                && !skipLlm
                && !reply.shouldEnd
                && reply.understood
                && reply.replyLines != null
                && reply.replyLines.Count > 0)
            {
                DialogueHistory.Instance?.Add("小凌", q, "interview");
                ic.SetTranslatingPlaceholder(true);
                RefreshInterviewView();
                interviewLlmCo = StartCoroutine(PreferLlmInterviewReplyCo(q, reply, who));
                return;
            }

            if (llmReady && reply != null)
                ic.AppendSpeakerReply(reply);
            DialogueHistory.Instance?.Add("小凌", q, "interview");
            RecordInterviewReplyHistory(who, reply, reply?.replyLines);
            RefreshInterviewView();
        }

        void RecordInterviewReplyHistory(string who, InterviewReply reply, IList<string> lines)
        {
            if (reply == null || DialogueHistory.Instance == null) return;
            if (!string.IsNullOrEmpty(reply.behavior))
                DialogueHistory.Instance.Add("", "（" + reply.behavior + "）", "interview");
            if (lines == null) return;
            foreach (var line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                    DialogueHistory.Instance.Add(who, line.Trim(), "interview");
            }
        }

        /// <summary>Wait for DeepSeek; only then show lines. Rule text is fallback-only.</summary>
        IEnumerator PreferLlmInterviewReplyCo(string question, InterviewReply reply, string who)
        {
            var llm = LlmClient.Instance;
            var ic = InterviewController.Instance;
            var ruleLines = reply?.replyLines != null
                ? new List<string>(reply.replyLines)
                : new List<string>();

            if (llm == null || !llm.IsConfigured || ic == null || reply == null)
            {
                FinishInterviewReply(ic, reply, who, ruleLines, null);
                yield break;
            }

            bool omitRuleFacts = reply.isRepeat
                                 || LooksLikeStaleRepeatRule(ruleLines);
            string facts = "";
            if (!omitRuleFacts)
            {
                facts = string.Join("\n", ruleLines);
                if (!string.IsNullOrEmpty(reply.behavior))
                    facts = "行为：" + reply.behavior + "\n台词：\n" + facts;
            }
            else if (reply.cognitiveBoundary)
            {
                facts = "（认知边界：保持困惑，短答「不知道/那是什么」，勿解释人类医疗）";
            }
            var userMsg = ic.BuildFreeAnswerUserMessage(facts, question, reply);

            while (llm.IsCoolingDown)
            {
                if (mode != Mode.Interview)
                {
                    ic.SetTranslatingPlaceholder(false);
                    interviewLlmCo = null;
                    yield break;
                }
                float left = Mathf.Max(0.1f, llm.SecondsUntilReady);
                yield return new WaitForSecondsRealtime(Mathf.Min(0.5f, left));
            }

            string rephrased = null;
            yield return llm.RephraseCoroutine(ic.BuildStylePrompt(reply), userMsg, question, text => rephrased = text);

            if (mode != Mode.Interview || ic.Subject == InterviewSubject.None)
            {
                ic.SetTranslatingPlaceholder(false);
                interviewLlmCo = null;
                yield break;
            }

            var aiLines = string.IsNullOrWhiteSpace(rephrased) ? null : SplitLlmReplyLines(rephrased);
            string outcome;
            string detail = null;
            if (aiLines != null && !ic.AcceptRephrasedLines(aiLines, reply, out var reject))
            {
                detail = reject;
                Debug.LogWarning("[Interview] LLM rejected by design filter: " + reject
                    + "\nRule:\n" + string.Join("\n", ruleLines)
                    + "\nAI:\n" + rephrased);
                aiLines = null;
                outcome = "rejected_fallback_rule";
            }
            else if (aiLines != null && aiLines.Count > 0)
            {
                outcome = "ai_ok";
                Debug.Log("[Interview] LLM free answer ok (" + who + " / " + (reply.intent ?? "?") + ")\nQ: "
                    + question + "\nAI:\n" + string.Join("\n", aiLines));
            }
            else
            {
                outcome = "fallback_rule";
                detail = llm.LastError ?? "empty";
                Debug.Log("[Interview] LLM fallback to rule lines (" + who + "): " + detail);
            }

            InterviewDebugLog.Exchange(
                question,
                reply.intent,
                freeMode: true,
                ruleText: facts,
                aiText: rephrased,
                outcome: outcome,
                detail: detail);

            FinishInterviewReply(ic, reply, who, ruleLines, aiLines);
        }

        static bool LooksLikeStaleRepeatRule(IList<string> ruleLines)
        {
            if (ruleLines == null || ruleLines.Count == 0) return false;
            var joined = string.Join("", ruleLines);
            return joined.Contains("刚才说过了")
                   || joined.Contains("这段我刚才说过了")
                   || joined.Contains("没更多了")
                   || joined.Contains("换个问法");
        }

        void FinishInterviewReply(
            InterviewController ic,
            InterviewReply reply,
            string who,
            List<string> ruleLines,
            List<string> aiLines)
        {
            ic?.SetTranslatingPlaceholder(false);
            var lines = (aiLines != null && aiLines.Count > 0) ? aiLines : ruleLines;
            if (reply != null)
                reply.replyLines = lines != null ? new List<string>(lines) : new List<string>();
            ic?.AppendSpeakerReply(reply, lines);
            RecordInterviewReplyHistory(who, reply, lines);
            RefreshInterviewView();
            interviewLlmCo = null;
        }

        static List<string> SplitLlmReplyLines(string text)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(text))
                return result;
            var parts = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            foreach (var p in parts)
            {
                var line = p.Trim();
                if (line.Length == 0)
                    continue;
                if (line.StartsWith("大福：") || line.StartsWith("大福:"))
                    line = line.Substring(3).Trim();
                else if (line.StartsWith("林女士：") || line.StartsWith("林女士:"))
                    line = line.Substring(4).Trim();
                if (line.Length > 0)
                    result.Add(line);
            }
            if (result.Count == 0)
                result.Add(text.Trim());
            return result;
        }

        void FormatInterviewLog(StringBuilder sb)
        {
            var log = InterviewController.Instance.Log;
            foreach (var line in log)
            {
                if (line.StartsWith("小凌："))
                {
                    sb.AppendLine();
                    sb.Append("<color=#8B6914>").Append(EscapeRich(line)).Append("</color>\n");
                }
                else if (line.StartsWith("大福：") || line.StartsWith("林女士："))
                {
                    // Strip speaker prefix for notepad — red tag already names the subject.
                    var body = line;
                    int colon = line.IndexOf('：');
                    if (colon < 0) colon = line.IndexOf(':');
                    if (colon >= 0 && colon + 1 < line.Length)
                        body = line.Substring(colon + 1).Trim();
                    sb.Append("<color=#2A2218>").Append(EscapeRich(body)).Append("</color>\n");
                }
                else if (line.StartsWith("（") || line.StartsWith("("))
                {
                    sb.Append("<color=#6E655C>").Append(EscapeRich(line)).Append("</color>\n");
                }
                else if (line.StartsWith("系统"))
                {
                    sb.Append("<color=#8B4A2A>").Append(EscapeRich(line)).Append("</color>\n");
                }
                else
                {
                    sb.AppendLine(EscapeRich(line));
                }
            }
        }

        static string EscapeRich(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return s.Replace("<", "＜").Replace(">", "＞");
        }

        void UpdateInterviewMeters(InterviewerStats st)
        {
            if (st == null)
            {
                SetMeterFill(interviewTrustSegs, 0, IvTrust);
                SetMeterFill(interviewStressSegs, 0, IvStress);
                SetMeterFill(interviewFocusSegs, 0, IvFocus);
                if (interviewMeterCaption != null)
                    interviewMeterCaption.text = "";
                return;
            }

            SetMeterFill(interviewTrustSegs, st.trust, IvTrust);
            SetMeterFill(interviewStressSegs, st.stress, IvStress);
            SetMeterFill(interviewFocusSegs, st.attention, IvFocus);

            if (interviewMeterCaption != null)
            {
                var caption = st.StatusText ?? "";
                if (InterviewController.Instance != null && InterviewController.Instance.CanComplete())
                    caption += " · " + UiLoc.T("ui.interview.can_end", "可结束采访");
                interviewMeterCaption.text = caption;
            }
        }

        static void SetMeterFill(Image[] segs, int value0to100, Color fill)
        {
            if (segs == null) return;
            int filled = Mathf.Clamp(Mathf.RoundToInt(value0to100 / 100f * IvMeterSegments), 0, IvMeterSegments);
            for (int i = 0; i < segs.Length; i++)
            {
                if (segs[i] == null) continue;
                segs[i].color = i < filled ? fill : IvSegEmpty;
            }
        }

        void ApplyInterviewPortraits()
        {
            var ic = InterviewController.Instance;
            if (ic == null) return;

            // Free interview shows only the subject — no interviewer/companion CG.
            if (ic.Subject == InterviewSubject.Dafu)
                SetPortrait(VnArt.ResolvePortrait("大福", LineSpeaker.Character));
            else
                SetPortrait(VnArt.ResolvePortrait("林女士", LineSpeaker.Character));
            SetInterviewCompanionPortrait(null);
        }

        void SetInterviewCompanionPortrait(string portraitKey)
        {
            if (interviewCompanionPortrait == null) return;
            if (string.IsNullOrEmpty(portraitKey))
            {
                interviewCompanionPortrait.sprite = null;
                interviewCompanionPortrait.enabled = false;
                interviewCompanionPortrait.gameObject.SetActive(false);
                return;
            }

            var sprite = VnArt.GetPortrait(portraitKey);
            if (sprite == null)
            {
                interviewCompanionPortrait.sprite = null;
                interviewCompanionPortrait.enabled = false;
                interviewCompanionPortrait.gameObject.SetActive(false);
                return;
            }

            interviewCompanionPortrait.sprite = sprite;
            interviewCompanionPortrait.color = Color.white;
            interviewCompanionPortrait.enabled = true;
            interviewCompanionPortrait.gameObject.SetActive(true);
            interviewCompanionPortrait.preserveAspect = true;
        }

        void LayoutInterviewPortraitSlot(Sprite sprite)
        {
            if (portraitImage == null) return;

            // Solo subject slot (left). Right-side dual layout was for Lin+Dafu companion.
            const float slotLeft = 0.04f;
            const float slotRight = 0.40f;
            const float slotTop = 0.90f;
            const float slotBottom = 0.28f;
            float slotW = slotRight - slotLeft;
            float slotH = slotTop - slotBottom;

            float heightNorm = slotH;
            float widthNorm = slotW;
            if (sprite != null)
            {
                float aspect = sprite.rect.width / Mathf.Max(1f, sprite.rect.height);
                widthNorm = heightNorm * aspect;
                if (widthNorm > slotW)
                {
                    widthNorm = slotW;
                    heightNorm = widthNorm / aspect;
                }
            }

            float cx = (slotLeft + slotRight) * 0.5f;
            float left = cx - widthNorm * 0.5f;
            float right = cx + widthNorm * 0.5f;
            float bottom = slotBottom;
            float top = bottom + heightNorm;
            Stretch(portraitImage.rectTransform,
                new Vector2(left, bottom), new Vector2(right, top),
                Vector2.zero, Vector2.zero);
            portraitImage.preserveAspect = true;
        }

        public void ShowInterview(InterviewSubject subject, bool returnToWritingAfter = false)
        {
            mode = Mode.Interview;
            if (GameState.Instance != null)
                GameState.Instance.Data.uiMode = subject == InterviewSubject.Lin ? "interview_lin" : "interview_dafu";
            SetAdvanceEnabled(false);
            SaveSystem.Autosave();
            InterviewController.Instance.Begin(subject, returnToWritingAfter);

            SetProp(null);
            SetChrome(false, false, false);
            SetInterviewChrome(true);
            chapterChip.gameObject.SetActive(true);
            objectiveText.gameObject.SetActive(true);
            SetStageBackground(subject == InterviewSubject.Lin ? "咖啡馆_午后" : "保安亭_傍晚");
            RefreshHeader();

            if (interviewSubjectText != null)
            {
                interviewSubjectText.text = subject == InterviewSubject.Dafu
                    ? UiLoc.T("ui.interview.subject_dafu", "大福")
                    : UiLoc.T("ui.interview.subject_lin", "受访者");
            }

            interviewInput.gameObject.SetActive(true);
            interviewInput.text = "";
            if (interviewInput.placeholder is Text ph)
            {
                ph.text = UiLoc.T("ui.interview.input_placeholder", "输入你的问题...");
            }

            ClearButtons();
            RefreshInterviewView();
        }

        void RefreshInterviewView()
        {
            if (mode != Mode.Interview)
                mode = Mode.Interview;
            SetInterviewChrome(true);
            RefreshHeader();

            if (interviewSubjectText != null && InterviewController.Instance != null)
            {
                interviewSubjectText.text = InterviewController.Instance.Subject == InterviewSubject.Dafu
                    ? UiLoc.T("ui.interview.subject_dafu", "大福")
                    : UiLoc.T("ui.interview.subject_lin", "受访者");
            }

            var sb = new StringBuilder();
            if (InterviewController.Instance != null && InterviewController.Instance.IsTranslating)
            {
                sb.Append("<color=#6E655C>")
                    .Append(UiLoc.T("ui.interview.translating", "……"))
                    .Append("</color>");
            }
            else
            {
                FormatInterviewLog(sb);
            }
            interviewLogText.text = sb.ToString().TrimEnd();
            ApplyLetterSpacing(interviewLogText, 0f);
            Canvas.ForceUpdateCanvases();
            if (interviewScroll != null)
                interviewScroll.verticalNormalizedPosition = 0f;

            UpdateInterviewMeters(InterviewController.Instance?.Stats);

            ClearInterviewChromeButtons();
            ApplyStageArt();
            ApplyInterviewPortraits();

            AddInterviewAction(UiLoc.T("ui.interview.end", "结束采访"), TryEndInterview);
            if (InterviewController.Instance.IsReinterviewFromWriting)
                AddInterviewAction(UiLoc.T("ui.interview.back_writing", "返回写稿"), () =>
                {
                    SetInterviewChrome(false);
                    InterviewController.Instance.AbandonToWriting();
                });
            AddInterviewAction(UiLoc.T("ui.menu.backlog", "回看"), OpenBacklog);
            AddInterviewAction(UiLoc.T("ui.menu.notebook", "笔记"), OpenNotebook);
            AddInterviewAction(UiLoc.T("ui.menu", "菜单"), OpenMenu);

            RefreshInterviewHints();
        }

        void ClearInterviewPresets()
        {
            foreach (var go in interviewPresetSpawned)
                if (go) Destroy(go);
            interviewPresetSpawned.Clear();
        }

        void RefreshInterviewHints()
        {
            ClearInterviewPresets();
            if (InterviewController.Instance == null)
            {
                if (interviewHintRoot != null)
                    interviewHintRoot.gameObject.SetActive(false);
                if (interviewCoachTipText != null)
                    interviewCoachTipText.text = "";
                return;
            }

            var subject = InterviewController.Instance.Subject;
            if (subject == InterviewSubject.None)
            {
                if (interviewHintRoot != null)
                    interviewHintRoot.gameObject.SetActive(false);
                if (interviewCoachTipText != null)
                    interviewCoachTipText.text = "";
                return;
            }

            var bundle = InterviewHintService.GetHints(
                InterviewHintService.BuildContext(InterviewController.Instance));

            if (interviewCoachTipText != null)
            {
                interviewCoachTipText.text = bundle?.CoachTip ?? "";
                ApplyLetterSpacing(interviewCoachTipText, 0f);
            }

            var presets = bundle?.AskChips;
            if (interviewHintRoot == null)
                return;
            if (presets == null || presets.Count == 0)
            {
                interviewHintRoot.gameObject.SetActive(false);
                return;
            }

            interviewHintRoot.gameObject.SetActive(true);
            int shown = Mathf.Min(3, presets.Count);
            for (int i = 0; i < shown; i++)
                SpawnInterviewPresetChip(presets[i], i);
        }

        void RefreshInterviewPresets() => RefreshInterviewHints();

        void SpawnInterviewPresetChip(string question, int colorIdx)
        {
            if (interviewHintRoot == null || string.IsNullOrEmpty(question))
                return;

            string label = question.Length <= 12 ? question : question.Substring(0, 11) + "…";
            var go = new GameObject("Preset", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(interviewHintRoot, false);
            var paperColor = IvChipColors[colorIdx % IvChipColors.Length];
            go.GetComponent<Image>().color = paperColor;
            go.GetComponent<Image>().raycastTarget = true;
            var le = go.GetComponent<LayoutElement>();
            le.minHeight = 44f;
            le.preferredHeight = 48f;
            le.minWidth = 140f;
            le.preferredWidth = Mathf.Clamp(48f + label.Length * 16f, 160f, 280f);
            le.flexibleWidth = 1f;
            string fill = question;
            go.GetComponent<Button>().onClick.AddListener(() =>
            {
                SfxController.Instance?.PlayUi();
                FillInterviewInput(fill);
            });

            AttachPaperclip(go.transform, new Vector2(0.08f, 1.05f), 28f, 40f, 8f + colorIdx * 4f);

            var icon = CreateUiText(go.transform, "Icon", 16, TextAnchor.MiddleCenter, IvInk,
                Vector2.zero, Vector2.zero);
            var irt = icon.rectTransform;
            irt.anchorMin = new Vector2(0f, 0f);
            irt.anchorMax = new Vector2(0f, 1f);
            irt.pivot = new Vector2(0f, 0.5f);
            irt.anchoredPosition = new Vector2(14f, 0f);
            irt.sizeDelta = new Vector2(28f, 0f);
            icon.text = IvChipIcons[colorIdx % IvChipIcons.Length];

            var tgo = new GameObject("L", typeof(RectTransform));
            tgo.transform.SetParent(go.transform, false);
            Stretch(tgo.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(42f, 4f), new Vector2(-10f, -4f));
            var tx = tgo.AddComponent<Text>();
            tx.font = font;
            tx.fontSize = 15;
            tx.alignment = TextAnchor.MiddleLeft;
            tx.color = IvInk;
            tx.text = label;
            tx.raycastTarget = false;
            tx.horizontalOverflow = HorizontalWrapMode.Wrap;
            tx.verticalOverflow = VerticalWrapMode.Truncate;
            ApplyLetterSpacing(tx, 0f);
            interviewPresetSpawned.Add(go);
        }

        void FillInterviewInput(string question)
        {
            if (interviewInput == null || string.IsNullOrEmpty(question))
                return;
            if (!interviewInput.gameObject.activeInHierarchy)
                return;
            interviewInput.text = question;
            interviewInput.ActivateInputField();
            interviewInput.caretPosition = interviewInput.text.Length;
            if (statusText)
                statusText.text = UiLoc.T("ui.interview.preset_filled", "已填入预设提问，可修改后发送");
            if (interviewCoachTipText != null)
                interviewCoachTipText.text = UiLoc.T("ui.interview.preset_filled", "已填入预设提问，可修改后发送");
        }

        void TryEndInterview()
        {
            if (!InterviewController.Instance.CanComplete())
            {
                var msg = UiLoc.T("ui.interview.end_warn", "现在结束的话，似乎还有不少事情没有问清楚。")
                          + "\n\n" + InterviewController.Instance.MissingSummary();
                interviewLogText.text = interviewLogText.text
                    + "\n\n<color=#6A7A8A>" + EscapeRich(msg).Replace("\n", "\n") + "</color>";
                ApplyLetterSpacing(interviewLogText, 0f);
                Canvas.ForceUpdateCanvases();
                if (interviewScroll != null)
                    interviewScroll.verticalNormalizedPosition = 0f;

                ClearInterviewChromeButtons();
                AddInterviewAction(UiLoc.T("ui.interview.continue", "继续采访"), () => RefreshInterviewView());
                AddInterviewAction(UiLoc.T("ui.interview.confirm_end", "确认结束"), () =>
                {
                    SetInterviewChrome(false);
                    InterviewController.Instance.End(true);
                }, true);
                return;
            }
            SetInterviewChrome(false);
            InterviewController.Instance.End(true);
        }

        void ApplyInterviewFonts()
        {
            float scale = GameSettings.FontSizeScale;
            if (interviewLogText != null)
            {
                interviewLogText.font = font;
                interviewLogText.fontSize = Mathf.RoundToInt(20f * scale);
                interviewLogText.alignment = TextAnchor.UpperLeft;
                interviewLogText.horizontalOverflow = HorizontalWrapMode.Wrap;
                interviewLogText.color = IvInk;
                ApplyLetterSpacing(interviewLogText, 0f);
            }
            if (interviewSubjectText != null)
            {
                interviewSubjectText.font = font;
                interviewSubjectText.fontSize = Mathf.RoundToInt(15f * scale);
            }
            if (interviewMeterCaption != null)
            {
                interviewMeterCaption.font = font;
                interviewMeterCaption.fontSize = Mathf.RoundToInt(12f * scale);
            }
            if (interviewInput != null)
            {
                if (interviewInput.textComponent != null)
                {
                    interviewInput.textComponent.font = font;
                    interviewInput.textComponent.fontSize = Mathf.RoundToInt(17f * scale);
                    interviewInput.textComponent.color = IvInk;
                    ApplyLetterSpacing(interviewInput.textComponent, 0f);
                }
                if (interviewInput.placeholder is Text ph)
                {
                    ph.font = font;
                    ph.fontSize = Mathf.RoundToInt(16f * scale);
                    ApplyLetterSpacing(ph, 0f);
                }
            }
        }
    }
}

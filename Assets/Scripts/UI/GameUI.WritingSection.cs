using StreetCat.Loc;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StreetCat.UI
{
    /// <summary>
    /// Writing-desk skin from Assets/Art/UI/writing_section.
    /// Virtual canvas is 1678×937; pieces use source_rect plus the 4px export pad.
    /// </summary>
    public partial class GameUI
    {
        const string WritingSectionRoot = "VnArt/UI/WritingSection/";
        const float WsCanvasW = 1678f;
        const float WsCanvasH = 937f;
        const float WsPad = 4f;

        static readonly Color WsInk = new Color(0.29f, 0.14f, 0.07f, 1f);
        static readonly Color WsMuted = new Color(0.46f, 0.33f, 0.24f, 1f);
        static readonly Color WsCream = new Color(1f, 0.95f, 0.86f, 1f);
        static readonly Color WsPaperFill = new Color(0.91f, 0.83f, 0.72f, 1f);

        static readonly System.Collections.Generic.Dictionary<string, Sprite> WritingSectionCache
            = new System.Collections.Generic.Dictionary<string, Sprite>();

        bool TryApplyWritingSectionSkin()
        {
            if (writingDeskRoot == null) return false;
            var article = LoadWritingSectionSprite("02_without_text/01_article_panel");
            if (article == null) return false;

            writingDeskArtOn = true;
            wdDirOnSprite = LoadWritingSectionSprite("02_without_text/09_direction_selected");
            wdDirOffSprite = LoadWritingSectionSprite("02_without_text/10_direction_unselected");

            var desk = FindImage(writingDeskRoot, "Desk");
            if (desk != null)
            {
                desk.sprite = null;
                desk.color = new Color(0f, 0f, 0f, 0.001f);
                desk.raycastTarget = true;
            }

            var paper = FindImage(writingDeskRoot, "Paper");
            if (paper != null)
            {
                paper.sprite = null;
                paper.color = Color.clear;
                paper.raycastTarget = false;
            }

            HideWritingDeskProceduralChrome();
            var board = EnsureWritingDeskBoard();
            board.SetAsLastSibling();
            BuildWritingSectionPieces(board);
            LayoutWritingSectionChrome();
            HideWritingDeskHud();
            return true;
        }

        void HideWritingDeskProceduralChrome()
        {
            string[] hide =
            {
                "Paper/HeadlineCover",
                "Paper/LeftColumn/DraftLabel",
                "Paper/LeftColumn/CharCount",
                "Paper/LeftColumn/SourcesLabel",
                "Paper/LeftColumn/Sources",
                "Paper/RightColumn/S1",
                "Paper/RightColumn/S3",
                "Paper/RightColumn/StatusIcon",
                "Paper/VRule"
            };
            for (int i = 0; i < hide.Length; i++)
            {
                var t = writingDeskRoot.transform.Find(hide[i]);
                if (t != null) t.gameObject.SetActive(false);
            }

            var paper = writingDeskRoot.transform.Find("Paper");
            if (paper != null)
            {
                for (int i = 0; i < paper.childCount; i++)
                {
                    var child = paper.GetChild(i);
                    if (child.name.StartsWith("Line"))
                        child.gameObject.SetActive(false);
                }
            }

            if (wdDraftCharCount != null)
                wdDraftCharCount.gameObject.SetActive(false);
            if (wdSourcesLine != null)
                wdSourcesLine.gameObject.SetActive(false);
        }

        void HideWritingDeskHud()
        {
            if (topBarImage != null)
                topBarImage.gameObject.SetActive(false);
            if (hudActionsRoot != null)
                hudActionsRoot.SetActive(false);
            if (hudSkipChip != null)
                hudSkipChip.SetActive(false);
        }

        RectTransform EnsureWritingDeskBoard()
        {
            var existing = writingDeskRoot.transform.Find("Board");
            if (existing != null)
            {
                writingDeskBoard = existing as RectTransform;
                existing.gameObject.SetActive(true);
                return writingDeskBoard;
            }

            var go = new GameObject("Board", typeof(RectTransform), typeof(AspectRatioFitter));
            go.transform.SetParent(writingDeskRoot.transform, false);
            writingDeskBoard = go.GetComponent<RectTransform>();
            StretchFull(writingDeskBoard);
            var fitter = go.GetComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = WsCanvasW / WsCanvasH;
            return writingDeskBoard;
        }

        void BuildWritingSectionPieces(RectTransform board)
        {
            EnsureWsImage(board, "PaperBoard", "04_backgrounds/paper_board_transparent",
                15f, 45f, 1640f, 876f, 0f);
            EnsureWsImage(board, "ArticlePanel", "02_without_text/01_article_panel",
                82f, 85f, 1004f, 727f);
            EnsureWsImage(board, "DirectionPanel", "02_without_text/06_direction_panel",
                1119f, 110f, 521f, 217f);
            EnsureWsImage(board, "MaterialsPanel", "02_without_text/07_materials_panel",
                1119f, 344f, 527f, 241f);
            EnsureWsImage(board, "StatusPanel", "02_without_text/08_status_panel",
                1120f, 598f, 532f, 206f);
            EnsureWsImage(board, "BinderClip", "03_icons/binder_clip",
                1335f, 16f, 78f, 91f, 0f);

            wdMatsEmptyCover = EnsureWsImage(board, "MatsCover", null,
                1142f, 417f, 480f, 153f, 0f);
            wdMatsEmptyCover.sprite = null;
            wdMatsEmptyCover.color = WsPaperFill;
            wdMatsEmptyCover.enabled = false;

            SkinWritingSectionButton("BackMats", "02_without_text/11_return_button",
                70f, 829f, 371f, 69f, 56f, WsInk);
            SkinWritingSectionButton("PreviewDesk", "02_without_text/12_preview_button",
                459f, 831f, 321f, 70f, 56f, WsInk);
            SkinWritingSectionButton("AiPolish", "02_without_text/13_edit_button",
                798f, 830f, 353f, 71f, 56f, WsInk);
            SkinWritingSectionButton("Submit", "02_without_text/14_submit_button",
                1170f, 827f, 453f, 73f, 56f, WsCream);

            SkinWritingSectionDir("DirGuard", 1173f, 185f, 418f, 55f);
            SkinWritingSectionDir("DirRescue", 1174f, 246f, 416f, 55f);

            var draftRow = writingDeskRoot.transform.Find("Paper/LeftColumn/DraftRow") as RectTransform;
            AdoptToBoard(draftRow);
            PlaceBoardRect(draftRow, 122f, 269f, 925f, 469f, 0f);

            AdoptToBoard(wdKicker != null ? wdKicker.rectTransform : null);
            AdoptToBoard(wdDate != null ? wdDate.rectTransform : null);
            AdoptToBoard(wdHeadline != null ? wdHeadline.rectTransform : null);
            AdoptToBoard(wdMatsCount != null ? wdMatsCount.rectTransform : null);
            AdoptToBoard(wdMatsList != null ? wdMatsList.rectTransform : null);
            AdoptToBoard(wdMatsHint != null ? wdMatsHint.rectTransform : null);
            AdoptToBoard(wdStatusLines != null ? wdStatusLines.rectTransform : null);

            wdDirHeading = EnsureWsText(board, "DirHeading", 27, TextAnchor.MiddleLeft, WsInk, true);
            wdMatsHeading = EnsureWsText(board, "MatsHeading", 27, TextAnchor.MiddleLeft, WsInk, true);
            wdStatusHeading = EnsureWsText(board, "StatusHeading", 27, TextAnchor.MiddleLeft, WsInk, true);
            wdStep1 = EnsureWsText(board, "Step1", 30, TextAnchor.MiddleCenter, WsCream, true);
            wdStep2 = EnsureWsText(board, "Step2", 30, TextAnchor.MiddleCenter, WsCream, true);
            wdStep3 = EnsureWsText(board, "Step3", 30, TextAnchor.MiddleCenter, WsCream, true);
            wdSrcObs = EnsureWsText(board, "SrcObs", 17, TextAnchor.MiddleLeft, WsInk, true);
            wdSrcCat = EnsureWsText(board, "SrcCat", 17, TextAnchor.MiddleLeft, WsInk, true);
            wdSrcHuman = EnsureWsText(board, "SrcHuman", 17, TextAnchor.MiddleLeft, WsInk, true);
            wdStatusDirTx = EnsureWsText(board, "StatusDir", 19, TextAnchor.MiddleLeft, WsInk, false);
            wdStatusParaTx = EnsureWsText(board, "StatusPara", 19, TextAnchor.MiddleLeft, WsInk, false);

            EnsureWritingSectionMenu(board);

            var leftoverBar = writingDeskRoot.transform.Find("ActionBar");
            if (leftoverBar != null)
            {
                leftoverBar.GetComponent<Image>().raycastTarget = false;
                leftoverBar.gameObject.SetActive(false);
            }
        }

        void EnsureWritingSectionMenu(RectTransform board)
        {
            var existing = board.Find("DeskMenu");
            Button btn;
            TextMeshProUGUI label;
            if (existing == null)
            {
                var go = new GameObject("DeskMenu", typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(board, false);
                btn = go.GetComponent<Button>();
                btn.onClick.AddListener(() =>
                {
                    SfxController.Instance?.PlayUi();
                    OpenMenu();
                });
                label = CreateUiText(go.transform, "T", 12, TextAnchor.MiddleCenter, WsInk,
                    Vector2.zero, Vector2.zero);
                Stretch(label.rectTransform, new Vector2(0f, -0.28f), new Vector2(1f, 0.18f),
                    Vector2.zero, Vector2.zero);
                label.raycastTarget = false;
            }
            else
            {
                btn = existing.GetComponent<Button>();
                label = existing.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            var img = btn.GetComponent<Image>();
            var spr = LoadWritingSectionSprite("02_without_text/25_menu_button");
            if (spr != null)
            {
                img.sprite = spr;
                img.color = Color.white;
                img.type = Image.Type.Simple;
                img.preserveAspect = true;
            }
            img.raycastTarget = true;
            PlaceBoardRect(btn.transform as RectTransform, 1609f, 14f, 54f, 56f);
            var menuCaption = EnsureWsText(board, "MenuLabel", 12, TextAnchor.MiddleCenter, WsInk, false);
            PlaceBoardRect(menuCaption.rectTransform, 1609f, 68f, 54f, 18f, 0f);
            menuCaption.text = UiLoc.T("ui.menu", "菜单");
            if (label != null)
                label.gameObject.SetActive(false);
        }

        void SkinWritingSectionButton(string name, string spritePath,
            float x, float y, float w, float h, float iconPad, Color labelColor)
        {
            var t = writingDeskRoot.transform.Find("ActionBar/" + name)
                    ?? (writingDeskBoard != null ? writingDeskBoard.Find(name) : null);
            if (t == null) return;
            AdoptToBoard(t);
            PlaceBoardRect(t as RectTransform, x, y, w, h);
            var img = t.GetComponent<Image>();
            var spr = LoadWritingSectionSprite(spritePath);
            if (img != null && spr != null)
            {
                img.sprite = spr;
                img.color = Color.white;
                img.type = Image.Type.Simple;
                img.preserveAspect = false;
                img.raycastTarget = true;
            }
            var label = t.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.gameObject.SetActive(true);
                label.color = labelColor;
                label.fontStyle = FontStyles.Bold;
                label.enableWordWrapping = true;
                label.overflowMode = TextOverflowModes.Overflow;
                Stretch(label.rectTransform, Vector2.zero, Vector2.one,
                    new Vector2(iconPad, 6f), new Vector2(-12f, -6f));
            }
            t.gameObject.SetActive(true);
        }

        void SkinWritingSectionDir(string name, float x, float y, float w, float h)
        {
            var t = writingDeskRoot.transform.Find("Paper/RightColumn/" + name)
                    ?? (writingDeskBoard != null ? writingDeskBoard.Find(name) : null);
            if (t == null) return;
            AdoptToBoard(t);
            PlaceBoardRect(t as RectTransform, x, y, w, h);
            var img = t.GetComponent<Image>();
            if (img != null)
            {
                img.color = Color.white;
                img.type = Image.Type.Simple;
                img.preserveAspect = false;
                img.raycastTarget = true;
            }
            var label = t.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.alignment = VnText.ToAlignment(TextAnchor.MiddleLeft);
                label.color = WsInk;
                Stretch(label.rectTransform, Vector2.zero, Vector2.one,
                    new Vector2(18f, 4f), new Vector2(-48f, -4f));
            }
            t.gameObject.SetActive(true);
        }

        void LayoutWritingSectionChrome()
        {
            if (writingDeskBoard == null) return;

            PlaceBoardRect(wdKicker != null ? wdKicker.rectTransform : null, 116f, 103f, 590f, 42f, 0f);
            PlaceBoardRect(wdDate != null ? wdDate.rectTransform : null, 934f, 108f, 131f, 36f, 0f);
            PlaceBoardRect(wdHeadline != null ? wdHeadline.rectTransform : null, 117f, 175f, 943f, 53f, 0f);
            PlaceBoardRect(wdDirHeading != null ? wdDirHeading.rectTransform : null, 1208f, 129f, 366f, 35f, 0f);
            PlaceBoardRect(wdMatsHeading != null ? wdMatsHeading.rectTransform : null, 1208f, 362f, 334f, 34f, 0f);
            PlaceBoardRect(wdMatsCount != null ? wdMatsCount.rectTransform : null, 1547f, 369f, 54f, 28f, 0f);
            PlaceBoardRect(wdStatusHeading != null ? wdStatusHeading.rectTransform : null, 1208f, 616f, 383f, 33f, 0f);
            PlaceBoardRect(wdStep1 != null ? wdStep1.rectTransform : null, 1143.5f, 120.5f, 49f, 49f, 0f);
            PlaceBoardRect(wdStep2 != null ? wdStep2.rectTransform : null, 1143.5f, 357.5f, 49f, 49f, 0f);
            PlaceBoardRect(wdStep3 != null ? wdStep3.rectTransform : null, 1143.5f, 610.5f, 49f, 49f, 0f);
            PlaceBoardRect(wdSrcObs != null ? wdSrcObs.rectTransform : null, 282f, 766f, 201f, 30f, 0f);
            PlaceBoardRect(wdSrcCat != null ? wdSrcCat.rectTransform : null, 565f, 766f, 145f, 30f, 0f);
            PlaceBoardRect(wdSrcHuman != null ? wdSrcHuman.rectTransform : null, 797f, 766f, 244f, 30f, 0f);
            PlaceBoardRect(wdStatusLines != null ? wdStatusLines.rectTransform : null, 1212f, 680f, 280f, 27f, 0f);
            PlaceBoardRect(wdStatusDirTx != null ? wdStatusDirTx.rectTransform : null, 1212f, 713f, 280f, 27f, 0f);
            PlaceBoardRect(wdStatusParaTx != null ? wdStatusParaTx.rectTransform : null, 1212f, 745f, 300f, 27f, 0f);

            var srcWord = writingDeskBoard.Find("SourceWord") as RectTransform;
            if (srcWord == null)
            {
                var tx = EnsureWsText(writingDeskBoard, "SourceWord", 24, TextAnchor.MiddleLeft, WsInk, true);
                srcWord = tx.rectTransform;
            }
            PlaceBoardRect(srcWord, 113f, 764f, 110f, 31f, 0f);

            if (wdKicker != null)
            {
                wdKicker.gameObject.SetActive(true);
                wdKicker.alignment = VnText.ToAlignment(TextAnchor.MiddleLeft);
                wdKicker.fontStyle = FontStyles.Bold;
                wdKicker.color = WsInk;
                wdKicker.fontSize = 26;
                wdKicker.enableWordWrapping = false;
            }
            if (wdDate != null)
            {
                wdDate.gameObject.SetActive(true);
                wdDate.alignment = VnText.ToAlignment(TextAnchor.MiddleRight);
                wdDate.color = WsMuted;
                wdDate.fontSize = 22;
            }
            if (wdHeadline != null)
            {
                wdHeadline.alignment = VnText.ToAlignment(TextAnchor.MiddleLeft);
                wdHeadline.fontStyle = FontStyles.Bold;
                wdHeadline.color = WsInk;
                wdHeadline.fontSize = 36;
                wdHeadline.enableWordWrapping = false;
                wdHeadline.overflowMode = TextOverflowModes.Overflow;
            }
            if (wdDirHeading != null)
            {
                wdDirHeading.text = UiLoc.T("ui.writing.desk.dir_heading", "WRITING DIRECTION");
                wdDirHeading.fontSize = 22;
            }
            if (wdMatsHeading != null)
            {
                wdMatsHeading.text = UiLoc.T("ui.writing.desk.mats_heading", "SELECTED MATERIALS");
                wdMatsHeading.fontSize = 22;
            }
            if (wdStatusHeading != null)
            {
                wdStatusHeading.text = UiLoc.T("ui.writing.desk.status_heading", "GENERATION STATUS");
                wdStatusHeading.fontSize = 22;
            }
            if (wdStep1 != null) { wdStep1.text = "1"; wdStep1.fontSize = 26; }
            if (wdStep2 != null) { wdStep2.text = "2"; wdStep2.fontSize = 26; }
            if (wdStep3 != null) { wdStep3.text = "3"; wdStep3.fontSize = 26; }
            if (wdSrcObs != null)
            {
                wdSrcObs.text = UiLoc.T("ui.writing.desk.source_obs", "ON-SITE OBSERVATION");
                wdSrcObs.fontSize = 14;
            }
            if (wdSrcCat != null)
            {
                wdSrcCat.text = UiLoc.T("ui.writing.desk.source_cat", "CAT MEMORIES");
                wdSrcCat.fontSize = 14;
            }
            if (wdSrcHuman != null)
            {
                wdSrcHuman.text = UiLoc.T("ui.writing.desk.source_human", "HUMAN INTERVIEWS");
                wdSrcHuman.fontSize = 14;
            }
            if (wdMatsCount != null)
            {
                wdMatsCount.alignment = VnText.ToAlignment(TextAnchor.MiddleRight);
                wdMatsCount.color = WsMuted;
                wdMatsCount.fontSize = 18;
            }
            if (wdMatsList != null)
            {
                wdMatsList.alignment = VnText.ToAlignment(TextAnchor.UpperLeft);
                wdMatsList.color = WsInk;
                wdMatsList.fontSize = 16;
                wdMatsList.enableWordWrapping = true;
            }
            if (wdMatsHint != null)
            {
                wdMatsHint.alignment = VnText.ToAlignment(TextAnchor.MiddleCenter);
                wdMatsHint.color = WsMuted;
                wdMatsHint.fontSize = 16;
            }
            if (wdStatusLines != null)
            {
                wdStatusLines.alignment = VnText.ToAlignment(TextAnchor.MiddleLeft);
                wdStatusLines.color = WsInk;
                wdStatusLines.fontSize = 16;
                wdStatusLines.enableWordWrapping = false;
            }
            if (wdDraftBody != null)
            {
                wdDraftBody.color = WsInk;
                wdDraftBody.fontSize = 20;
                wdDraftBody.lineSpacing = 12f;
            }
            if (wdDraftInput != null && wdDraftInput.placeholder is TextMeshProUGUI ph)
            {
                ph.color = new Color(WsMuted.r, WsMuted.g, WsMuted.b, 0.85f);
                ph.fontSize = 18;
            }

            var draftRow = writingDeskBoard.Find("DraftRow") as RectTransform;
            PlaceBoardRect(draftRow, 122f, 269f, 925f, 469f, 0f);
            if (wdDraftScroll != null)
            {
                var hostImg = wdDraftScroll.GetComponent<Image>();
                if (hostImg != null)
                    hostImg.color = Color.clear;
            }

            ApplyWritingSectionButtonLabels();
        }

        void ApplyWritingSectionButtonLabels()
        {
            SetBarLabel("BackMats", UiLoc.T("ui.writing.desk.back_mats_art", "RETURN TO RESELECT MATERIALS"));
            SetBarLabel("PreviewDesk", UiLoc.T("ui.writing.desk.preview_art", "PREVIEW ARTICLE"));
            if (wdPolishLabel != null && (writingPolishCo == null) && !writingAiPolishUsed)
                wdPolishLabel.text = UiLoc.T("ui.writing.desk.edit_art", "EDIT KEY STATEMENTS");
            if (wdSubmitLabel != null && wdSubmitBtn != null && wdSubmitBtn.interactable)
                wdSubmitLabel.text = UiLoc.T("ui.writing.desk.submit_art", "SUBMIT TO EDITOR-IN-CHIEF FOR REVIEW");
        }

        void SetBarLabel(string name, string text)
        {
            if (writingDeskBoard == null) return;
            var t = writingDeskBoard.Find(name);
            var label = t != null ? t.GetComponentInChildren<TextMeshProUGUI>(true) : null;
            if (label != null) label.text = text;
        }

        void RefreshWritingSectionDynamic(int matCount, int coveredParas, bool canAssemble, bool polishing)
        {
            if (!writingDeskArtOn) return;

            if (wdMatsEmptyCover != null)
                wdMatsEmptyCover.enabled = matCount > 0;

            if (wdMatsHint != null)
            {
                if (matCount == 0)
                {
                    PlaceBoardRect(wdMatsHint.rectTransform, 1190f, 526f, 405f, 30f, 0f);
                    wdMatsHint.alignment = VnText.ToAlignment(TextAnchor.MiddleCenter);
                    wdMatsHint.gameObject.SetActive(true);
                }
                else
                {
                    wdMatsHint.gameObject.SetActive(false);
                }
            }

            if (wdMatsList != null)
            {
                PlaceBoardRect(wdMatsList.rectTransform, 1160f, 430f, 440f, 130f, 0f);
                wdMatsList.gameObject.SetActive(matCount > 0);
            }

            if (wdStatusLines != null)
            {
                wdStatusLines.text = canAssemble
                    ? UiLoc.T("ui.writing.desk.st_ready", "Ready to generate")
                    : UiLoc.T("ui.writing.desk.st_not_ready", "Not ready yet");
                if (polishing)
                    wdStatusLines.text = UiLoc.T("ui.writing.desk.st_polishing", "Polishing…");
            }
            if (wdStatusDirTx != null)
                wdStatusDirTx.text = UiLoc.T("ui.writing.desk.st_dir", "Direction selected");
            if (wdStatusParaTx != null)
            {
                wdStatusParaTx.text = coveredParas >= 4
                    ? UiLoc.T("ui.writing.desk.st_mats", "All four paragraphs have cards")
                    : UiLoc.T("ui.writing.desk.st_mats_low", "Some paragraphs still empty");
            }

            if (wdKicker != null)
                wdKicker.text = UiLoc.T("ui.writing.desk.kicker_banner", "HUAI'AN COMMUNITY FEATURE STORY");

            var srcWord = writingDeskBoard != null ? writingDeskBoard.Find("SourceWord") : null;
            var srcTx = srcWord != null ? srcWord.GetComponent<TextMeshProUGUI>() : null;
            if (srcTx != null)
                srcTx.text = UiLoc.T("ui.writing.desk.source_label", "SOURCE:");

            ApplyWritingSectionButtonLabels();
            if (wdPolishLabel != null)
            {
                if (polishing)
                    wdPolishLabel.text = UiLoc.T("ui.writing.desk.ai_polishing", "AI 优化中…");
                else if (writingAiPolishUsed)
                    wdPolishLabel.text = UiLoc.T("ui.writing.desk.ai_polish_used", "已优化");
                else
                    wdPolishLabel.text = UiLoc.T("ui.writing.desk.edit_art", "EDIT KEY STATEMENTS");
            }
            if (wdSubmitLabel != null)
            {
                if (!canAssemble)
                    wdSubmitLabel.text = UiLoc.T("ui.writing.desk.submit_locked", "尚不能提交");
                else if (polishing)
                    wdSubmitLabel.text = UiLoc.T("ui.writing.desk.submit_polishing", "优化中…");
                else
                    wdSubmitLabel.text = UiLoc.T("ui.writing.desk.submit_art", "SUBMIT TO EDITOR-IN-CHIEF FOR REVIEW");
            }
        }

        void AdoptToBoard(Transform t)
        {
            if (t == null || writingDeskBoard == null) return;
            t.SetParent(writingDeskBoard, false);
            t.gameObject.SetActive(true);
        }

        void PlaceBoardRect(RectTransform rt, float x, float y, float w, float h, float pad = 4f)
        {
            if (rt == null) return;
            float left = x - pad;
            float top = y - pad;
            float width = w + pad * 2f;
            float height = h + pad * 2f;
            rt.anchorMin = new Vector2(left / WsCanvasW, 1f - (top + height) / WsCanvasH);
            rt.anchorMax = new Vector2((left + width) / WsCanvasW, 1f - top / WsCanvasH);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        Image EnsureWsImage(RectTransform board, string name, string spritePath,
            float x, float y, float w, float h, float pad = 4f)
        {
            var t = board.Find(name);
            Image img;
            if (t == null)
                img = CreateImage(board, name, Color.white);
            else
                img = t.GetComponent<Image>() ?? t.gameObject.AddComponent<Image>();

            if (!string.IsNullOrEmpty(spritePath))
            {
                var spr = LoadWritingSectionSprite(spritePath);
                if (spr != null)
                {
                    img.sprite = spr;
                    img.color = Color.white;
                    img.type = Image.Type.Simple;
                    img.preserveAspect = false;
                }
            }
            img.raycastTarget = false;
            PlaceBoardRect(img.rectTransform, x, y, w, h, pad);
            img.gameObject.SetActive(true);
            return img;
        }

        TextMeshProUGUI EnsureWsText(RectTransform board, string name, int size, TextAnchor align,
            Color color, bool bold)
        {
            var t = board.Find(name);
            TextMeshProUGUI tx;
            if (t != null)
                tx = t.GetComponent<TextMeshProUGUI>();
            else
                tx = CreateUiText(board, name, size, align, color, Vector2.zero, Vector2.zero);
            tx.fontSize = size;
            tx.color = color;
            tx.alignment = VnText.ToAlignment(align);
            tx.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            tx.enableWordWrapping = false;
            tx.overflowMode = TextOverflowModes.Overflow;
            tx.raycastTarget = false;
            return tx;
        }

        static Sprite LoadWritingSectionSprite(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return null;
            if (WritingSectionCache.TryGetValue(relativePath, out var cached))
                return cached;
            var resourcePath = WritingSectionRoot + relativePath;
            var sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite == null)
            {
                var texture = Resources.Load<Texture2D>(resourcePath);
                if (texture != null)
                    sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f), 100f);
            }
            WritingSectionCache[relativePath] = sprite;
            return sprite;
        }
    }
}

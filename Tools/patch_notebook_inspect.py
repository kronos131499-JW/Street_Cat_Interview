# -*- coding: utf-8 -*-
from pathlib import Path

path = Path(r"D:\Street_Cat_Interview\github\Street_Cat_Interview\Assets\Scripts\UI\GameUI.cs")
text = path.read_text(encoding="utf-8")

old = r'''        void ResumeOverlayReturn()
        {
            waitingForChoice = savedWaitingForChoice;
            switch (returnFromOverlay)
            {
                case Mode.Dialogue:
                    mode = Mode.Dialogue;
                    SetAdvanceEnabled(!waitingForChoice, waitingForChoice);
                    statusText.text = waitingForChoice ? "做出选择" : "点击继续　·　Ctrl / 跳过";
                    break;
                case Mode.Investigate: ShowInvestigationMode(); break;
                case Mode.Talk: ShowTalkMenu(); break;
                case Mode.Interview: RefreshInterviewView(); break;
                case Mode.Writing: ShowWritingDirectionPick(); break;
                case Mode.Epilogue: ShowEpilogue(); break;
                case Mode.Notebook: OpenNotebook(); break;
                default:
                    mode = Mode.Dialogue;
                    SetAdvanceEnabled(true);
                    break;
            }
        }'''

new = r'''        void ResumeOverlayReturn()
        {
            waitingForChoice = savedWaitingForChoice;
            var dest = returnFromOverlay;
            // Never resume into overlay modes (would appear as a no-op / loop).
            if (dest == Mode.Notebook || dest == Mode.Menu || dest == Mode.Backlog || dest == Mode.Title)
            {
                var ui = GameState.Instance != null ? GameState.Instance.Data.uiMode : "";
                if (ui == "investigate") dest = Mode.Investigate;
                else if (!string.IsNullOrEmpty(ui) && ui.StartsWith("interview")) dest = Mode.Interview;
                else if (ui == "writing") dest = Mode.Writing;
                else if (ui == "epilogue") dest = Mode.Epilogue;
                else dest = Mode.Dialogue;
            }

            switch (dest)
            {
                case Mode.Dialogue:
                    mode = Mode.Dialogue;
                    SetInvestigateChrome(false);
                    SetInterviewChrome(false);
                    SetChrome(true, false, true);
                    ClearButtons();
                    AddAction("跳过", TrySkipDialogue);
                    AddAction("回看", OpenBacklog);
                    AddAction("笔记", OpenNotebook);
                    AddAction("菜单", OpenMenu);
                    SetAdvanceEnabled(!waitingForChoice, waitingForChoice);
                    statusText.text = waitingForChoice ? "做出选择" : "点击继续　·　Ctrl / 跳过";
                    break;
                case Mode.Investigate: ShowInvestigationMode(); break;
                case Mode.Talk: ShowTalkMenu(); break;
                case Mode.Interview: RefreshInterviewView(); break;
                case Mode.Writing: ShowWritingDirectionPick(); break;
                case Mode.Epilogue: ShowEpilogue(); break;
                default:
                    mode = Mode.Dialogue;
                    SetChrome(true, false, true);
                    SetAdvanceEnabled(true);
                    break;
            }
        }'''

if old not in text:
    raise SystemExit("ResumeOverlayReturn block not found")
text = text.replace(old, new, 1)

old_nb = r'''            AddAction("回看", OpenBacklog);
            AddAction("菜单", OpenMenu);
            AddAction("返回", () =>
            {
                ResumeOverlayReturn();
            }, true);
        }
    }
}'''

new_nb = r'''            AddAction("回看", OpenBacklog);
            AddAction("菜单", OpenMenu);
            AddAction("返回", CloseNotebook, true);
        }

        void CloseNotebook()
        {
            // Explicit exit so we never treat Notebook as the resume target.
            if (returnFromOverlay == Mode.Notebook || returnFromOverlay == Mode.Menu || returnFromOverlay == Mode.Backlog)
            {
                var ui = GameState.Instance != null ? GameState.Instance.Data.uiMode : "";
                if (ui == "investigate") returnFromOverlay = Mode.Investigate;
                else if (!string.IsNullOrEmpty(ui) && ui.StartsWith("interview")) returnFromOverlay = Mode.Interview;
                else if (ui == "writing") returnFromOverlay = Mode.Writing;
                else returnFromOverlay = Mode.Dialogue;
            }
            ResumeOverlayReturn();
        }
    }
}'''

if old_nb not in text:
    raise SystemExit("OpenNotebook footer not found")
text = text.replace(old_nb, new_nb, 1)

old_hi = r'''        void ShowHotspotInspect(string hotspotId)
        {
            var service = InvestigationService.Instance;
            lastInspectText = service.Inspect(hotspotId);
            SfxController.Instance?.PlayInspect();

            mode = Mode.Investigate;
            if (GameState.Instance != null)
                GameState.Instance.Data.uiMode = "investigate";
            SetAdvanceEnabled(false);
            inputField.gameObject.SetActive(false);
            SetInvestigateChrome(false);
            chapterChip.gameObject.SetActive(true);
            objectiveText.gameObject.SetActive(true);
            locationText.text = "槐安社区";
            RefreshHeader();
            ApplyAtmosphere();

            SetSpeaker("小凌", LineSpeaker.Inner);
            SetBody(lastInspectText, true, "investigate");
            statusText.text = "调查结果";
            ClearButtons();
            AddAction("返回调查", ShowInvestigationMode, true);
            AddAction("笔记", OpenNotebook);
            AddAction("菜单", OpenMenu);
        }'''

new_hi = r'''        readonly List<InspectBeat> inspectQueue = new List<InspectBeat>();
        int inspectIndex;

        void ShowHotspotInspect(string hotspotId)
        {
            var service = InvestigationService.Instance;
            lastInspectText = service.Inspect(hotspotId);
            inspectQueue.Clear();
            inspectQueue.AddRange(service.GetInspectBeats(hotspotId));
            if (inspectQueue.Count == 0)
                inspectQueue.Add(new InspectBeat { narration = true, text = lastInspectText });
            inspectIndex = 0;
            SfxController.Instance?.PlayInspect();

            mode = Mode.Investigate;
            if (GameState.Instance != null)
                GameState.Instance.Data.uiMode = "investigate";
            SetAdvanceEnabled(false);
            inputField.gameObject.SetActive(false);
            SetInvestigateChrome(false);
            chapterChip.gameObject.SetActive(true);
            objectiveText.gameObject.SetActive(true);
            locationText.text = "槐安社区";
            RefreshHeader();
            ApplyAtmosphere();
            ShowInspectBeat();
        }

        void ShowInspectBeat()
        {
            if (inspectIndex < 0 || inspectIndex >= inspectQueue.Count)
            {
                ShowInvestigationMode();
                return;
            }

            var beat = inspectQueue[inspectIndex];
            if (beat.narration)
                SetSpeaker("", LineSpeaker.Narration);
            else
                SetSpeaker("小凌", LineSpeaker.Character);

            SetBody(beat.text, true, beat.narration ? "narration" : "investigate");
            statusText.text = $"调查　{inspectIndex + 1}/{inspectQueue.Count}";
            ClearButtons();
            bool last = inspectIndex >= inspectQueue.Count - 1;
            if (last)
                AddAction("返回调查", ShowInvestigationMode, true);
            else
                AddAction("继续", () => { inspectIndex++; ShowInspectBeat(); }, true);
            AddAction("笔记", OpenNotebook);
            AddAction("菜单", OpenMenu);
        }'''

if old_hi not in text:
    raise SystemExit("ShowHotspotInspect not found")
text = text.replace(old_hi, new_hi, 1)

text = text.replace(
    '小凌（内心）　" + msg',
    '" + msg',
)
text = text.replace(
    'SetSpeaker("小凌", LineSpeaker.Inner);\n            SetBody(sb.ToString());',
    'SetSpeaker("", LineSpeaker.Narration);\n            SetBody(sb.ToString());',
)

path.write_text(text, encoding="utf-8")
print("GameUI patched OK")

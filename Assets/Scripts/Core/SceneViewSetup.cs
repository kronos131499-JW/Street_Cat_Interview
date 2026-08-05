using StreetCat.UI;
using UnityEngine;

namespace StreetCat
{
    public class SceneViewSetup : MonoBehaviour
    {
        void Start()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                go.tag = "MainCamera";
                cam = go.GetComponent<Camera>();
            }
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.Lerp(VnTheme.BgTop, VnTheme.BgBottom, 0.35f);
            cam.orthographic = true;
        }
    }
}

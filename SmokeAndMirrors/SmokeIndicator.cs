using System;

using UnityEngine;
using UnityEngine.UI;

namespace SmokeAndMirrors
{
    [DisallowMultipleComponent]
    class SmokeIndicator : MonoBehaviour
    {
        public Transform AfterburnerIndicator { get; set; }

        private UIImageToggle imgToggle;

        private void Awake()
        {
            try
            {
                imgToggle = GetComponent<UIImageToggle>();

                GetComponent<MeshFilter>().mesh = AfterburnerIndicator.GetComponent<MeshFilter>().mesh;
                GetComponent<MeshRenderer>().material = AfterburnerIndicator.GetComponent<MeshRenderer>().material;

                var image = transform.Find("Image");
                var abImage = AfterburnerIndicator.transform.Find("Image");

                image.GetComponent<Image>().sprite = abImage.GetComponent<Image>().sprite;

                var label = image.Find("smokeLabel");
                var abLabel = abImage.Find("abLabel");

                label.GetComponent<MeshFilter>().mesh = new Mesh();
                label.GetComponent<MeshRenderer>().material = abLabel.GetComponent<MeshRenderer>().material;
                label.GetComponent<VTText>().font = abLabel.GetComponent<VTText>().font;
                label.GetComponent<VTText>().fontSize = 18f;
            }
            catch (NullReferenceException)
            {
                Main.LogError($"Could not configure indicator for {transform}");
            }
        }

        public void SetState(bool shouldLightBeOn)
        {
            if (imgToggle != null)
            {
                imgToggle.imageEnabled = shouldLightBeOn;
            }
        }
    }
}

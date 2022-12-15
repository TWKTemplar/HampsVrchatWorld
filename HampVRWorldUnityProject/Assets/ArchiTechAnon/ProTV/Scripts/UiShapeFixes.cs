
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace ArchiTech
{
    public class UiShapeFixes : UdonSharpBehaviour
    {
        void Start()
        {
            var uiShapes = GetComponentsInChildren(typeof(VRC_UiShape), true);
            foreach (Component uiShape in uiShapes)
            {
                var box = uiShape.GetComponent<BoxCollider>();
                if (box != null)
                {
                    var rect = (RectTransform)uiShape.transform;
                    box.isTrigger = true;
                    box.size = new Vector3(rect.sizeDelta.x, rect.sizeDelta.y, 0);
                }
            }
        }
    }
}

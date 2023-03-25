using UnityEngine;
using UnityEngine.UI;

namespace SimplestarGame
{
    public class CursorController : MonoBehaviour
    {
        [SerializeField] Image cursorImage;
        [SerializeField] TouchInput touchInput;

        void Start()
        {
            if (Application.isMobilePlatform)
            {
                this.cursorImage.enabled = false;
                return;
            }
            Cursor.visible = false;
            this.cursorTransform = this.cursorImage.GetComponent<RectTransform>();
            if (null != this.touchInput)
            {
                this.touchInput.onLeftTap += this.OnLeftTap;
                this.touchInput.onRightTap += this.OnRightTap;
            }
        }

        void OnLeftTap(Vector2 point)
        {
            this.cursorTransform.localPosition = point - 0.5f * new Vector2(Screen.width, Screen.height);
        }

        void OnRightTap(Vector2 point)
        {
            this.cursorTransform.localPosition = point - 0.5f * new Vector2(Screen.width, Screen.height);
        }

        void Update()
        {
            if (Application.isMobilePlatform)
            {
                return;
            }
            Vector2 cursorPos = Input.mousePosition;
            this.cursorTransform.localPosition = cursorPos - 0.5f * new Vector2(Screen.width, Screen.height);
            bool isOutside = Screen.width < cursorPos.x || Screen.height < cursorPos.y || 0 > cursorPos.x || 0 > cursorPos.y;
            Cursor.visible = isOutside;
        }

        RectTransform cursorTransform;
    }
}
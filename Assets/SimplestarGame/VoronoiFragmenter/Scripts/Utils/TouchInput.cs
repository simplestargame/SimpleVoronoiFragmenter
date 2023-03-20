using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;
using UnityEngine.UI;
using System.Collections;

namespace SimplestarGame
{
    public class TouchInput : MonoBehaviour
    {
        [SerializeField] RectTransform circleOutlineL;
        [SerializeField] RectTransform circleOutlineR;
        [SerializeField] RectTransform circleButtonL;
        [SerializeField] RectTransform circleButtonR;
        [SerializeField, Tooltip("for scale source.")] Canvas canvas;
        [SerializeField, Tooltip("button max move distance")] float maxMoveDistance = 25f;
        [SerializeField] float maxTapTime = 0.16f;
        [SerializeField] Vector2 rightAxisScale = new Vector2(0.25f, -0.1f);

        [Space(10)]
        public bool debug;

        internal Action<Vector2> onRightAxis;
        internal Action<Vector2> onLeftAxis;
        internal Action<Vector2> onRightTap;
        internal Action<Vector2> onLeftTap;

        void Awake()
        {
            Application.targetFrameRate = 60;
            this.firstMaxDistance = this.maxMoveDistance;
        }

        void OnEnable()
        {
            EnhancedTouchSupport.Enable();
        }

        void OnDisable()
        {
            EnhancedTouchSupport.Disable();
        }

        bool IsScreenLeft(Vector2 screenPos)
        {
            return screenPos.x < Screen.width * 0.5f;
        }

        void Update()
        {
            this.maxMoveDistance = this.firstMaxDistance * canvas.scaleFactor;
#if DEBUG
            if (this.debug)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    bool isLeft = this.IsScreenLeft(Input.mousePosition);
                    var outline = isLeft ? this.circleOutlineL : this.circleOutlineR;
                    var button = isLeft ? this.circleButtonL : this.circleButtonR;
                    outline.position = button.position = Input.mousePosition;
                    this.dictionary[0] = new ButtonSet { outline = outline, button = button, beginTime = Time.time };
                }
                foreach (var keyValue in this.dictionary)
                {
                    var set = keyValue.Value;
                    if (this.maxTapTime < Time.time - set.beginTime)
                    {
                        set.outline.gameObject.SetActive(true);
                        set.button.gameObject.SetActive(true);

                        var distance = Mathf.Min(Vector3.Distance(set.outline.position, Input.mousePosition), this.maxMoveDistance);
                        var axis = (Input.mousePosition - set.outline.position).normalized * distance / this.maxMoveDistance;
                        set.button.position = set.outline.position + axis * this.maxMoveDistance;

                        if (this.IsScreenLeft(set.outline.position))
                        {
                            this.onLeftAxis?.Invoke(axis);
                        }
                        else
                        {
                            this.onRightAxis?.Invoke(axis * this.rightAxisScale);
                        }
                    }
                }
                if (Input.GetMouseButtonUp(0))
                {
                    if (this.dictionary.TryGetValue(0, out ButtonSet set))
                    {
                        set.outline.gameObject.SetActive(false);
                        set.button.gameObject.SetActive(false);
                        this.dictionary.Remove(0);
                        // Reset Axis
                        if (this.IsScreenLeft(set.outline.position))
                        {
                            this.onLeftAxis?.Invoke(Vector2.zero);
                        }
                        else
                        {
                            this.onRightAxis?.Invoke(Vector2.zero);
                        }
                        // Tap
                        if (this.maxTapTime > Time.time - set.beginTime)
                        {
                            Vector2 tapPos = set.outline.position;
                            if (this.IsScreenLeft(set.outline.position))
                            {
                                this.onLeftTap?.Invoke(tapPos);
                                if(this.circleOutlineL.TryGetComponent(out Image image))
                                {
                                    this.circleOutlineL.gameObject.SetActive(true);
                                    StartCoroutine(this.CoActivateObject(this.circleOutlineL.gameObject, false, this.maxTapTime));
                                }
                            }
                            else
                            {
                                this.onRightTap?.Invoke(tapPos);
                                if (this.circleOutlineR.TryGetComponent(out Image image))
                                {
                                    this.circleOutlineR.gameObject.SetActive(true);
                                    StartCoroutine(this.CoActivateObject(this.circleOutlineR.gameObject, false, this.maxTapTime));
                                }
                            }
                        }
                    }
                }
                return;
            }
#endif
            foreach (var touch in Touch.activeTouches)
            {
                switch (touch.phase)
                {
                    case TouchPhase.None:
                        break;
                    case TouchPhase.Began:
                        {
                            bool isLeft = this.IsScreenLeft(touch.screenPosition);
                            var outline = isLeft ? this.circleOutlineL : this.circleOutlineR;
                            var button = isLeft ? this.circleButtonL : this.circleButtonR;
                            outline.position = touch.screenPosition;
                            button.position = touch.screenPosition;
                            this.dictionary[touch.touchId] = new ButtonSet { outline = outline, button = button, beginTime = Time.time };
                        }
                        break;
                    case TouchPhase.Moved:
                        {                           
                            if (this.dictionary.TryGetValue(touch.touchId, out ButtonSet set))
                            {
                                if (this.maxTapTime < Time.time - set.beginTime)
                                {
                                    set.outline.gameObject.SetActive(true);
                                    set.button.gameObject.SetActive(true);

                                    var distance = Mathf.Min(Vector3.Distance(set.outline.position, touch.screenPosition), this.maxMoveDistance);
                                    var axis = (touch.screenPosition - new Vector2(set.outline.position.x, set.outline.position.y)).normalized * distance / this.maxMoveDistance;
                                    set.button.position = new Vector2(set.outline.position.x, set.outline.position.y) + axis * this.maxMoveDistance;

                                    if (this.IsScreenLeft(set.outline.position))
                                    {
                                        this.onLeftAxis?.Invoke(axis);
                                    }
                                    else
                                    {
                                        this.onRightAxis?.Invoke(axis * this.rightAxisScale);
                                    }
                                }
                            }
                        }
                        break;
                    case TouchPhase.Ended:
                        {
                            if (this.dictionary.TryGetValue(touch.touchId, out ButtonSet set))
                            {
                                set.outline.gameObject.SetActive(false);
                                set.button.gameObject.SetActive(false);
                                this.dictionary.Remove(touch.touchId);
                                // Reset Axis
                                if (this.IsScreenLeft(set.outline.position))
                                {
                                    this.onLeftAxis?.Invoke(Vector2.zero);
                                }
                                else
                                {
                                    this.onRightAxis?.Invoke(Vector2.zero);
                                }
                                // Tap
                                if (this.maxTapTime > Time.time - set.beginTime)
                                {
                                    Vector2 tapPos = set.outline.position;
                                    if (this.IsScreenLeft(set.outline.position))
                                    {
                                        this.onLeftTap?.Invoke(tapPos);
                                        if (this.circleOutlineL.TryGetComponent(out Image image))
                                        {
                                            this.circleOutlineL.gameObject.SetActive(true);
                                            StartCoroutine(this.CoActivateObject(this.circleOutlineL.gameObject, false, this.maxTapTime));
                                        }
                                    }
                                    else
                                    {
                                        this.onRightTap?.Invoke(tapPos);
                                        if (this.circleOutlineR.TryGetComponent(out Image image))
                                        {
                                            this.circleOutlineR.gameObject.SetActive(true);
                                            StartCoroutine(this.CoActivateObject(this.circleOutlineR.gameObject, false, this.maxTapTime));
                                        }
                                    }
                                }
                            }
                        }
                        break;
                }
            }
        }

        IEnumerator CoActivateObject(GameObject go, bool isActive, float waitTime)
        {
            yield return new WaitForSeconds(waitTime);
            go.SetActive(isActive);
        }

        class ButtonSet
        {
            public float beginTime;
            public RectTransform outline;
            public RectTransform button;
        }

        float firstMaxDistance;
        Dictionary<int, ButtonSet> dictionary = new Dictionary<int, ButtonSet>();
    }
}
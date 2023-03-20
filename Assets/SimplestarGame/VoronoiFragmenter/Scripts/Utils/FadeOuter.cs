using System.Collections;
using UnityEngine;

namespace SimplestarGame
{
    public class FadeOuter : MonoBehaviour
    {
        internal void FadeOut(float remainingTime, float duration, AnimationCurve curve)
        {
            this.transparencyId = Shader.PropertyToID("_Transparency");
            if (this.TryGetComponent(out Renderer renderer))
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                this.material = renderer.material;
                if (this.material.HasFloat(this.transparencyId))
                {
                    this.initTransparency = this.material.GetFloat(this.transparencyId);
                }
                StartCoroutine(this.CoFadeOut(remainingTime, duration, curve));
            }
        }

        IEnumerator CoFadeOut(float remainingTime, float duration, AnimationCurve curve)
        {
            yield return new WaitForSeconds(remainingTime);
            if (0f < this.initTransparency)
            {
                float elapsedTime = 0f;
                while (elapsedTime < duration)
                {
                    elapsedTime += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsedTime / duration);
                    t = curve.Evaluate(t);
                    float transparency = Mathf.Lerp(this.initTransparency, 0f, t);
                    this.material.SetFloat(this.transparencyId, transparency);
                    yield return null;
                }
            }
            Destroy(this.gameObject);
        }

        float initTransparency = 0f;
        int transparencyId;
        Material material;
    }
}
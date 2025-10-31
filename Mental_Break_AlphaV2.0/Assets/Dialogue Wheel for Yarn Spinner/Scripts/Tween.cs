namespace Yarn.Unity.Addons.DialogueWheel.Extensions {
    using System;
    using System.Collections;
    using UnityEngine;
    using UnityEngine.UI;

    #nullable enable

    internal static class TweenExtensions {
        
        public static Coroutine Tween<T>(this MonoBehaviour monoBehaviour, T from, T to, float duration, System.Action<T, T, float> apply, System.Action? onComplete) {
            return monoBehaviour.StartCoroutine(RunTween(from, to, duration, apply, onComplete));
        }

        private static IEnumerator RunTween<T>(T from, T to, float duration, Action<T, T, float> apply, Action? onComplete)
        {
            apply(from, to, 0);

            float elapsed = 0;

            while (elapsed <= duration) {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                apply(from, to, t);
                yield return null;
            }

            apply(from, to, 1);

            onComplete?.Invoke();
        }

        public static Coroutine CrossfadeImage(this Image image, Sprite toSprite, float duration, Action? onComplete = null) {
            var sourceImage = image;
            var destinationImage = image.gameObject.GetOrCreateChildWithComponent<Image>("Crossfade Image");

            destinationImage.preserveAspect = image.preserveAspect;
            destinationImage.sprite = toSprite;
            destinationImage.material = image.material;

            Color baseColor = image.color;
            baseColor.a = 1f;

            destinationImage.gameObject.SetActive(true);

            return image.Tween(0f, 1f, duration, (a, b, t) =>
            {
                // Color fromColor = baseColor;
                // fromColor.a = baseColor.a * (1f-t);

                Color toColor = baseColor;
                toColor.a = baseColor.a * t;

                // sourceImage.color = fromColor;
                destinationImage.color = toColor;
            }, () => {
                sourceImage.sprite = toSprite;
                sourceImage.color = baseColor;

                destinationImage.color = Color.clear;
                destinationImage.gameObject.SetActive(false);

                onComplete?.Invoke();
            });
        }

        private static T GetOrCreateChildWithComponent<T>(this GameObject target, string name, bool withRectTransform = true) where T : Component {
            GameObject childObject;
            var childTransform = target.transform.Find(name);
            if (childTransform != null) {
                childObject = childTransform.gameObject;
            } else {
                var newGO = new GameObject(name);
                newGO.transform.SetParent(target.transform, worldPositionStays: false);
                childObject = newGO;

                if (withRectTransform) {
                    var rectTransform = newGO.AddComponent<RectTransform>();
                    rectTransform.localScale = Vector3.one;

                    rectTransform.anchorMin = Vector2.zero;
                    rectTransform.anchorMax = Vector2.one;
                    
                    rectTransform.sizeDelta = Vector2.zero;
                    
                    rectTransform.anchoredPosition = Vector2.zero;
                }
            }

            if (childObject.TryGetComponent<T>(out var result)) {
                return result;
            } else {
                return childObject.AddComponent<T>();
            }
        }
    }
}
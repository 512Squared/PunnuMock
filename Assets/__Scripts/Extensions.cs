using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace __Scripts
{
    public static class Extensions
    {
        private static Random rng = new();

        public static Vector2 GetImageSize(this RectTransform image)
        {
            var height = image.sizeDelta.y;
            var width = image.sizeDelta.x;
            return new Vector2(width, height);
        }

        /// <summary>
        /// Destroys all child game objects
        /// </summary>
        /// <param name="transform"></param>
        /// <returns></returns>
        public static void Clear(this Transform transform)
        {
            foreach (Transform cd in transform) Object.Destroy(cd.gameObject);
        }

        public static void Clear(this RectTransform transform)
        {
            foreach (RectTransform cd in transform) Object.Destroy(cd.gameObject);
        }

        public static List<RectTransform> GetChildObjects(this RectTransform transform)
        {
            List<RectTransform> transforms = new();
            transforms.AddRange(transform.Cast<RectTransform>());
            return transforms;
        }

        /// <summary>
        /// Shuffles a list into a random order
        /// </summary>
        /// <param name="list"></param>
        /// <typeparam name="T"></typeparam>
        public static void Shuffle<T>(this IList<T> list)
        {
            var n = list.Count;
            while (n > 1)
            {
                n--;
                var k = rng.Next(n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
        }


        /// <summary>
        /// Populate any dropdown with any enum with this method
        /// </summary>
        /// <param name="dropdown"></param>
        /// <param name="targetEnum"></param>
        /// <param name="activeLang"></param>
        public static void PopulateDropDownWithEnum
            (this Dropdown dropdown, Enum targetEnum, IEnumerable<int> activeLang)
        {
            var enumType = targetEnum.GetType();
            List<Dropdown.OptionData> newOptions
                = activeLang.Select(t => new Dropdown.OptionData(Enum.GetName(enumType, t))).ToList();

            dropdown.ClearOptions();
            dropdown.AddOptions(newOptions);
        }


        public static Transform GetChildContainer(this GameObject hexCellParent)
        {
            var parentTransform = hexCellParent.transform.parent;
            if (parentTransform == null) return null; // no parent, so no siblings

            var index = hexCellParent.transform.GetSiblingIndex();
            if (index >= parentTransform.childCount - 1) return null; // no more siblings after this

            var nextSiblingTransform = parentTransform.GetChild(index + 1);
            return nextSiblingTransform != null ? nextSiblingTransform : null;
        }
        public static T FindFirstChildWithComponent<T>(this Transform parent) where T : Component
        {
            foreach (Transform child in parent)
            {
                var component = child.GetComponent<T>();
                if (component != null)
                {
                    return component;
                }

                var grandchildComponent = child.FindFirstChildWithComponent<T>();
                if (grandchildComponent != null)
                {
                    return grandchildComponent;
                }
            }

            return null;
        }

        /// <summary>
        /// Extracts the L2 text elements and colors them for display (uses lang_start tags etc)
        /// </summary>
        /// <param name="response"></param>
        /// <param name="openTag"></param>
        /// <param name="closeTag"></param>
        /// <param name="matches"></param>
        /// <param name="currentIndex"></param>
        /// <returns></returns>
        public static string ExtractLangTags (this string response, string openTag, string closeTag, int currentIndex, out MatchCollection matches)
        {
            var regex = new Regex($@"{openTag}(.*?){closeTag}", RegexOptions.Singleline);
            var matchCollection = regex.Matches(response);
            matches = matchCollection;

            var strippedResponse = regex.Replace(response, match =>
            {
                var result = $"<color=#00BF0D><link={currentIndex}>{match.Groups[1].Value}</link></color>"; 
                Debug.Log($"{result}");
                currentIndex++;
                return result;
            });

            return strippedResponse;
        }


        /// <summary>
        /// Adds color to identify speaker
        /// </summary>
        /// <param name="message"></param>
        /// <param name="isChatBot"></param>
        /// <returns></returns>
        public static string IdentifySpeakerByColor(this string message, bool isChatBot)
        {
            message = isChatBot
                ? Regex.Replace(message, @"^((?<!<color=#FFOOFF>)(Holly: )?)", "<color=#FF00FF>Holly:</color> ")
                : Regex.Replace(message, @"^((?<!<color=#DEE000>)(Ellen: )?)", "<color=#DEE000>Ellen:</color> ");

            return message;
        }
        
        public static Dictionary<string, string> ExtractSentencesByLabels(this string text)
        {
            Debug.Log("Extraction started");
            var sentencesByLabels = new Dictionary<string, string>();

            var labels = new[] { "Word:", "A1:", "A2:", "B1:", "B2:", "C1:", "C2:" };
            var currentLabel = string.Empty;
            var remainingText = text;

            foreach (var label in labels)
            {
                var labelIndex = remainingText.IndexOf(label, StringComparison.Ordinal);
                if (labelIndex >= 0)
                {
                    if (!string.IsNullOrEmpty(currentLabel))
                    {
                        sentencesByLabels[currentLabel] = remainingText.Substring(0, labelIndex).Trim();
                    }
                    currentLabel = label;
                    remainingText = remainingText.Substring(labelIndex + label.Length);
                }
            }

            if (!string.IsNullOrEmpty(currentLabel))
            {
                sentencesByLabels[currentLabel] = remainingText.Trim();
            }

            Debug.Log($"Dictionary count: {sentencesByLabels.Count}");

            return sentencesByLabels;
        }

        public static string ToRelativePath(this string fullPath)
        {
            var assetsIndex = fullPath.IndexOf("Assets", StringComparison.OrdinalIgnoreCase);
            return assetsIndex >= 0 ? fullPath[assetsIndex..].Replace('\\', '/') : fullPath;
        }
        
    }
}

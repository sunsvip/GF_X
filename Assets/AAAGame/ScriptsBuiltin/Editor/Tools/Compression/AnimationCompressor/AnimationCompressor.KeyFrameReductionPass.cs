using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AnimationCompressor
{
    public partial class Core
    {
        private void KeyFrameReductionPass()
        {
            var curveBindings = AnimationUtility.GetCurveBindings(compressClip);

            foreach (var curveBinding in curveBindings)
            {
                var isTansformCurve = Util.IsTransformKey(curveBinding.propertyName);
                var compressCurve = AnimationUtility.GetEditorCurve(compressClip, curveBinding);

                // Only working on transform keys
                if (isTansformCurve == false)
                    continue;

                KeyFrameReduction(curveBinding, compressCurve);
                compressClip.SetCurve(curveBinding.path, curveBinding.type, curveBinding.propertyName, compressCurve);
            }
        }

        /// <summary>
        /// 없어도될 키프레임 제거
        /// </summary>
        /// <param name="originCurve"></param>
        /// <param name="compressCurve"></param>
        /// <param name="allowErrorRange"></param>
        private void KeyFrameReduction(EditorCurveBinding curveBinding, AnimationCurve compressCurve)
        {
            var keys = compressCurve.keys;
            if (keys.Length <= 2)
                return;

            var propertyName = curveBinding.propertyName;
            var path = curveBinding.path;
            var depth = Util.GetDepth(path);
            var allowErrorRange = GetAllowErrorValue(propertyName, depth);
            var newKeyset = new List<Keyframe>();

            for (var i = 0; i < keys.Length - 1; i++)
            {
                var curKey = keys[i];
                var nextKey = keys[i + 1];

                float valueOffset;
                if (Util.IsRotaitonKey(propertyName))
                {
                    // TODO : 인-아웃 탄젠트/웨이트에 대한 오차값도 검사가 필요한가?
                    // 쿼터니언에 대한 ValueOffset 검사가 너무 루즈하게 되고 있음
                    // 로테이션 쪽만 뭔가 다르게 해야할듯
                    var curOffset = curKey.value * Mathf.Rad2Deg;
                    var nextOffset = nextKey.value * Mathf.Rad2Deg;
                    valueOffset = Mathf.Abs(curOffset - nextOffset);
                    valueOffset = float.MaxValue;
                }
                else
                {
                    valueOffset = Mathf.Abs(curKey.value - nextKey.value);
                }

                if (valueOffset >= allowErrorRange)
                {
                    newKeyset.Add(curKey);

                    if (i == keys.Length - 1)
                        newKeyset.Add(nextKey);
                }
                else
                {
                    var newValue = (curKey.value + nextKey.value) / 2f;
                    var newTime = (nextKey.time + curKey.time) / 2f;
                    var newKey = new Keyframe();

                    // Value, Time
                    newKey.value = newValue;
                    newKey.time = newTime;

                    // In
                    newKey.inTangent = curKey.inTangent;
                    newKey.inWeight = curKey.inWeight;

                    // Out
                    newKey.outTangent = nextKey.outTangent;
                    newKey.outWeight = nextKey.outWeight;
                }
            }

            compressCurve.keys = newKeyset.ToArray();
        }
    }
}
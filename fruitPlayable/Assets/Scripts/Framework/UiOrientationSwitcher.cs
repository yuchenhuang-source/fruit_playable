using System.Collections.Generic;
using UnityEngine;

public class UiOrientationSwitcher : MonoBehaviour
{
    [Header("将两套 UI 根节点拖拽到这里")]
    public List<GameObject> PortraitRoot; // 竖屏用内容
    public List<GameObject> LandscapeRoot; // 横屏用内容

    private void Start()
    {
        // 启动时立即应用一次
        Apply(OrientationWatcher.Instance.CurrentOrientation);

        // 订阅方向变化事件
        OrientationWatcher.Instance.OnOrientationChanged += HandleOrientationChanged;
    }

    private void OnDestroy()
    {
        if (OrientationWatcher.Instance != null)
        {
            OrientationWatcher.Instance.OnOrientationChanged -= HandleOrientationChanged;
        }
    }

    private void HandleOrientationChanged(AspectOrientation newOrientation, bool toPortrait, bool toLandscape)
    {
        Apply(newOrientation);
    }

    private void Apply(AspectOrientation orientation)
    {
        bool portrait = orientation == AspectOrientation.Portrait;
        if (PortraitRoot != null)
        {
            for (int i = 0; i < PortraitRoot.Count; i++)
            {
                PortraitRoot[i].SetActive(portrait);
            }
        }
        if (LandscapeRoot != null)
        {
            for (int i = 0; i < LandscapeRoot.Count; i++)
            {
                LandscapeRoot[i].SetActive(!portrait);
            }
        }
    }
}
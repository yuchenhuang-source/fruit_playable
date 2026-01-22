using UnityEngine;
using System;

public enum AspectOrientation
{
    Portrait,
    Landscape
}

// 将该组件挂到“常驻场景”的一个对象上（或用 DontDestroyOnLoad）
public class OrientationWatcher : MonoBehaviour
{
    public static OrientationWatcher Instance { get; private set; }

    // 当前判定的方向
    public AspectOrientation CurrentOrientation { get; private set; }

    // 当方向变化时触发：参数依次为 新方向、是否从横->竖、是否从竖->横
    public event Action<AspectOrientation, bool, bool> OnOrientationChanged;

    // 可选：当分辨率变化时触发（例如宽高变化但未跨越横竖临界）
    public event Action<int, int> OnResolutionChanged;

    // 记录上一次的宽高，便于检测变化
    private int _lastWidth;
    private int _lastHeight;

    // 用于避免同一帧多源重复触发
    private AspectOrientation _lastOrientationNotified;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 初始化
        _lastWidth = Screen.width;
        _lastHeight = Screen.height;
        CurrentOrientation = ComputeOrientation(Screen.width, Screen.height);
        _lastOrientationNotified = CurrentOrientation;
    }

    private void Update()
    {
        // 轮询分辨率变化（编辑器、桌面平台或窗口化时很有用）
        if (_lastWidth != Screen.width || _lastHeight != Screen.height)
        {
            _lastWidth = Screen.width;
            _lastHeight = Screen.height;

            OnResolutionChanged?.Invoke(_lastWidth, _lastHeight);

            TryNotifyOrientationChange();
        }
    }

    // 对于基于 UI 的项目，Canvas 根对象上尺寸变化会非常可靠
    // 注意：该回调需要组件挂在含有 RectTransform 的对象上（通常 Canvas 或其子物体）
    private void OnRectTransformDimensionsChange()
    {
        // 有些平台在旋转时会先触发这里，再更新 Screen.width/height
        // 为稳妥起见，仍然统一走 TryNotifyOrientationChange
        TryNotifyOrientationChange();
    }

    private void TryNotifyOrientationChange()
    {
        var newOrientation = ComputeOrientation(Screen.width, Screen.height);

        // 仅在发生横<->竖的跨越时触发
        if (newOrientation != _lastOrientationNotified)
        {
            bool toPortrait = newOrientation == AspectOrientation.Portrait && _lastOrientationNotified == AspectOrientation.Landscape;
            bool toLandscape = newOrientation == AspectOrientation.Landscape && _lastOrientationNotified == AspectOrientation.Portrait;

            CurrentOrientation = newOrientation;
            _lastOrientationNotified = newOrientation;

            OnOrientationChanged?.Invoke(newOrientation, toPortrait, toLandscape);
        }
    }

    private AspectOrientation ComputeOrientation(int w, int h)
    {
        // 这里用 >= 让正方形（w==h）归类为横屏，可按需改为 >
        return (w >= h) ? AspectOrientation.Landscape : AspectOrientation.Portrait;
    }

}
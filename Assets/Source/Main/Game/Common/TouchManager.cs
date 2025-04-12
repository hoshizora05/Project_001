using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// タッチのフェーズ
/// </summary>
public enum TouchPhaseEx
{
    Began,
    Moved,
    Stationary,
    Ended,
    Canceled
}

/// <summary>
/// タッチ・マウス入力の情報をまとめた構造体
/// </summary>
public struct TouchInfo
{
    public int fingerId;           // 指ID (マウスの場合は常に 0 など)
    public Vector2 position;       // 画面座標
    public TouchPhaseEx phase;     // タッチフェーズ
    public float deltaTime;        // 前回更新時からの経過時間
    public Vector2 deltaPosition;  // 前回タッチ座標からの差分

    public TouchInfo(int fingerId, Vector2 position, TouchPhaseEx phase, float deltaTime, Vector2 deltaPosition)
    {
        this.fingerId = fingerId;
        this.position = position;
        this.phase = phase;
        this.deltaTime = deltaTime;
        this.deltaPosition = deltaPosition;
    }
}

/// <summary>
/// タッチ入力を一括管理し、必要に応じてイベント通知するマネージャ
/// シングルタッチ・マルチタッチ・エディタ(マウス)の両方に対応
/// </summary>
public class TouchManager : MonoBehaviour
{
    // シングルトンパターン（必要に応じて）
    public static TouchManager Instance { get; private set; }

    // タッチ開始時に呼ばれるイベント
    public event Action<TouchInfo> OnTouchBegan;
    // タッチ移動中（または静止中）に呼ばれるイベント
    public event Action<TouchInfo> OnTouchMovedOrStationary;
    // タッチ終了時に呼ばれるイベント
    public event Action<TouchInfo> OnTouchEnded;

    // マルチタッチを識別するためのデータ管理
    private Dictionary<int, Vector2> _lastPositions = new Dictionary<int, Vector2>();
    private Dictionary<int, float> _lastTimes = new Dictionary<int, float>();

    private void Awake()
    {
        // シングルトン
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Update()
    {
        // 実機・ビルド時のタッチ情報を優先
        if (Input.touchSupported && Application.isMobilePlatform)
        {
            HandleTouches();
        }
        else
        {
            // エディタ環境などの場合はマウスで代替
            HandleMouseAsTouch();
        }
    }

    /// <summary>
    /// スマホ等でのマルチタッチ処理
    /// </summary>
    private void HandleTouches()
    {
        int touchCount = Input.touchCount;
        for (int i = 0; i < touchCount; i++)
        {
            UnityEngine.Touch unityTouch = Input.GetTouch(i);
            int fingerId = unityTouch.fingerId;
            Vector2 currentPos = unityTouch.position;

            // タッチフェーズを変換
            TouchPhaseEx phaseEx = ConvertTouchPhase(unityTouch.phase);

            // 前回情報の取り出しと更新
            Vector2 lastPos = _lastPositions.ContainsKey(fingerId) ? _lastPositions[fingerId] : currentPos;
            float lastTime = _lastTimes.ContainsKey(fingerId) ? _lastTimes[fingerId] : Time.time;

            Vector2 deltaPos = currentPos - lastPos;
            float deltaTime = Time.time - lastTime;

            // TouchInfo を生成
            TouchInfo touchInfo = new TouchInfo(
                fingerId,
                currentPos,
                phaseEx,
                deltaTime,
                deltaPos
            );

            // イベント発行
            DispatchTouchEvent(touchInfo);

            // 情報を記録
            if (phaseEx == TouchPhaseEx.Began)
            {
                if (!_lastPositions.ContainsKey(fingerId))
                {
                    _lastPositions.Add(fingerId, currentPos);
                }
                else
                {
                    _lastPositions[fingerId] = currentPos;
                }

                if (!_lastTimes.ContainsKey(fingerId))
                {
                    _lastTimes.Add(fingerId, Time.time);
                }
                else
                {
                    _lastTimes[fingerId] = Time.time;
                }
            }
            else if (phaseEx == TouchPhaseEx.Moved || phaseEx == TouchPhaseEx.Stationary)
            {
                _lastPositions[fingerId] = currentPos;
                _lastTimes[fingerId] = Time.time;
            }
            else if (phaseEx == TouchPhaseEx.Ended || phaseEx == TouchPhaseEx.Canceled)
            {
                // タッチ終了後は辞書から削除
                if (_lastPositions.ContainsKey(fingerId)) _lastPositions.Remove(fingerId);
                if (_lastTimes.ContainsKey(fingerId)) _lastTimes.Remove(fingerId);
            }
        }
    }

    /// <summary>
    /// エディタ用のマウス入力をタッチとして扱う
    /// </summary>
    private void HandleMouseAsTouch()
    {
        // fingerId は常に 0 として扱う
        int fingerId = 0;
        Vector2 currentPos = Input.mousePosition;
        Vector2 lastPos = _lastPositions.ContainsKey(fingerId) ? _lastPositions[fingerId] : currentPos;
        float lastTime = _lastTimes.ContainsKey(fingerId) ? _lastTimes[fingerId] : Time.time;

        Vector2 deltaPos = currentPos - lastPos;
        float deltaTime = Time.time - lastTime;

        // マウスのボタンが押された / 押し続けられている / 離された でフェーズを判定
        TouchPhaseEx phaseEx = TouchPhaseEx.Canceled; // デフォルト値
        if (Input.GetMouseButtonDown(0))
        {
            phaseEx = TouchPhaseEx.Began;
        }
        else if (Input.GetMouseButton(0))
        {
            // 移動していれば Moved、していなければ Stationary とする
            if (deltaPos.sqrMagnitude > 0.0001f)
            {
                phaseEx = TouchPhaseEx.Moved;
            }
            else
            {
                phaseEx = TouchPhaseEx.Stationary;
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            phaseEx = TouchPhaseEx.Ended;
        }
        else
        {
            // マウスが押されてない時はタッチなしとみなし、何もせず return
            return;
        }

        TouchInfo touchInfo = new TouchInfo(
            fingerId,
            currentPos,
            phaseEx,
            deltaTime,
            deltaPos
        );

        // イベント発行
        DispatchTouchEvent(touchInfo);

        // フェーズに応じて情報更新
        if (phaseEx == TouchPhaseEx.Began)
        {
            if (!_lastPositions.ContainsKey(fingerId))
            {
                _lastPositions.Add(fingerId, currentPos);
            }
            else
            {
                _lastPositions[fingerId] = currentPos;
            }

            if (!_lastTimes.ContainsKey(fingerId))
            {
                _lastTimes.Add(fingerId, Time.time);
            }
            else
            {
                _lastTimes[fingerId] = Time.time;
            }
        }
        else if (phaseEx == TouchPhaseEx.Moved || phaseEx == TouchPhaseEx.Stationary)
        {
            _lastPositions[fingerId] = currentPos;
            _lastTimes[fingerId] = Time.time;
        }
        else if (phaseEx == TouchPhaseEx.Ended)
        {
            if (_lastPositions.ContainsKey(fingerId)) _lastPositions.Remove(fingerId);
            if (_lastTimes.ContainsKey(fingerId)) _lastTimes.Remove(fingerId);
        }
    }

    /// <summary>
    /// Unityの TouchPhase を拡張版に変換
    /// </summary>
    private TouchPhaseEx ConvertTouchPhase(TouchPhase unityPhase)
    {
        switch (unityPhase)
        {
            case TouchPhase.Began:
                return TouchPhaseEx.Began;
            case TouchPhase.Moved:
                return TouchPhaseEx.Moved;
            case TouchPhase.Stationary:
                return TouchPhaseEx.Stationary;
            case TouchPhase.Ended:
                return TouchPhaseEx.Ended;
            case TouchPhase.Canceled:
                return TouchPhaseEx.Canceled;
            default:
                return TouchPhaseEx.Canceled;
        }
    }

    /// <summary>
    /// タッチフェーズに応じてイベントをディスパッチ
    /// </summary>
    private void DispatchTouchEvent(TouchInfo touchInfo)
    {
        switch (touchInfo.phase)
        {
            case TouchPhaseEx.Began:
                OnTouchBegan?.Invoke(touchInfo);
                break;
            case TouchPhaseEx.Moved:
            case TouchPhaseEx.Stationary:
                OnTouchMovedOrStationary?.Invoke(touchInfo);
                break;
            case TouchPhaseEx.Ended:
            case TouchPhaseEx.Canceled:
                OnTouchEnded?.Invoke(touchInfo);
                break;
        }
    }
}

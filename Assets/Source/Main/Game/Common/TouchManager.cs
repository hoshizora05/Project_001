using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// �^�b�`�̃t�F�[�Y
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
/// �^�b�`�E�}�E�X���͂̏����܂Ƃ߂��\����
/// </summary>
public struct TouchInfo
{
    public int fingerId;           // �wID (�}�E�X�̏ꍇ�͏�� 0 �Ȃ�)
    public Vector2 position;       // ��ʍ��W
    public TouchPhaseEx phase;     // �^�b�`�t�F�[�Y
    public float deltaTime;        // �O��X�V������̌o�ߎ���
    public Vector2 deltaPosition;  // �O��^�b�`���W����̍���

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
/// �^�b�`���͂��ꊇ�Ǘ����A�K�v�ɉ����ăC�x���g�ʒm����}�l�[�W��
/// �V���O���^�b�`�E�}���`�^�b�`�E�G�f�B�^(�}�E�X)�̗����ɑΉ�
/// </summary>
public class TouchManager : MonoBehaviour
{
    // �V���O���g���p�^�[���i�K�v�ɉ����āj
    public static TouchManager Instance { get; private set; }

    // �^�b�`�J�n���ɌĂ΂��C�x���g
    public event Action<TouchInfo> OnTouchBegan;
    // �^�b�`�ړ����i�܂��͐Î~���j�ɌĂ΂��C�x���g
    public event Action<TouchInfo> OnTouchMovedOrStationary;
    // �^�b�`�I�����ɌĂ΂��C�x���g
    public event Action<TouchInfo> OnTouchEnded;

    // �}���`�^�b�`�����ʂ��邽�߂̃f�[�^�Ǘ�
    private Dictionary<int, Vector2> _lastPositions = new Dictionary<int, Vector2>();
    private Dictionary<int, float> _lastTimes = new Dictionary<int, float>();

    private void Awake()
    {
        // �V���O���g��
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
        // ���@�E�r���h���̃^�b�`����D��
        if (Input.touchSupported && Application.isMobilePlatform)
        {
            HandleTouches();
        }
        else
        {
            // �G�f�B�^���Ȃǂ̏ꍇ�̓}�E�X�ő��
            HandleMouseAsTouch();
        }
    }

    /// <summary>
    /// �X�}�z���ł̃}���`�^�b�`����
    /// </summary>
    private void HandleTouches()
    {
        int touchCount = Input.touchCount;
        for (int i = 0; i < touchCount; i++)
        {
            UnityEngine.Touch unityTouch = Input.GetTouch(i);
            int fingerId = unityTouch.fingerId;
            Vector2 currentPos = unityTouch.position;

            // �^�b�`�t�F�[�Y��ϊ�
            TouchPhaseEx phaseEx = ConvertTouchPhase(unityTouch.phase);

            // �O����̎��o���ƍX�V
            Vector2 lastPos = _lastPositions.ContainsKey(fingerId) ? _lastPositions[fingerId] : currentPos;
            float lastTime = _lastTimes.ContainsKey(fingerId) ? _lastTimes[fingerId] : Time.time;

            Vector2 deltaPos = currentPos - lastPos;
            float deltaTime = Time.time - lastTime;

            // TouchInfo �𐶐�
            TouchInfo touchInfo = new TouchInfo(
                fingerId,
                currentPos,
                phaseEx,
                deltaTime,
                deltaPos
            );

            // �C�x���g���s
            DispatchTouchEvent(touchInfo);

            // �����L�^
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
                // �^�b�`�I����͎�������폜
                if (_lastPositions.ContainsKey(fingerId)) _lastPositions.Remove(fingerId);
                if (_lastTimes.ContainsKey(fingerId)) _lastTimes.Remove(fingerId);
            }
        }
    }

    /// <summary>
    /// �G�f�B�^�p�̃}�E�X���͂��^�b�`�Ƃ��Ĉ���
    /// </summary>
    private void HandleMouseAsTouch()
    {
        // fingerId �͏�� 0 �Ƃ��Ĉ���
        int fingerId = 0;
        Vector2 currentPos = Input.mousePosition;
        Vector2 lastPos = _lastPositions.ContainsKey(fingerId) ? _lastPositions[fingerId] : currentPos;
        float lastTime = _lastTimes.ContainsKey(fingerId) ? _lastTimes[fingerId] : Time.time;

        Vector2 deltaPos = currentPos - lastPos;
        float deltaTime = Time.time - lastTime;

        // �}�E�X�̃{�^���������ꂽ / �����������Ă��� / �����ꂽ �Ńt�F�[�Y�𔻒�
        TouchPhaseEx phaseEx = TouchPhaseEx.Canceled; // �f�t�H���g�l
        if (Input.GetMouseButtonDown(0))
        {
            phaseEx = TouchPhaseEx.Began;
        }
        else if (Input.GetMouseButton(0))
        {
            // �ړ����Ă���� Moved�A���Ă��Ȃ���� Stationary �Ƃ���
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
            // �}�E�X��������ĂȂ����̓^�b�`�Ȃ��Ƃ݂Ȃ��A�������� return
            return;
        }

        TouchInfo touchInfo = new TouchInfo(
            fingerId,
            currentPos,
            phaseEx,
            deltaTime,
            deltaPos
        );

        // �C�x���g���s
        DispatchTouchEvent(touchInfo);

        // �t�F�[�Y�ɉ����ď��X�V
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
    /// Unity�� TouchPhase ���g���łɕϊ�
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
    /// �^�b�`�t�F�[�Y�ɉ����ăC�x���g���f�B�X�p�b�`
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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FaceBlendShape : MonoBehaviour
{
    [SerializeField, Tooltip("カメラで撮影した顔情報を持つGameObject")]
    GameObject _faceDataObj = null;

    [SerializeField, Tooltip("顔情報を適用させる顔レンダーラー")]
    SkinnedMeshRenderer _faceSkin = null;

    [SerializeField, Tooltip("顔を動かすときのIKについて、ターゲットとするオブジェクト")]
    GameObject _ikFacePosTarget = null;

    [SerializeField, Tooltip("対象モデルのアニメータ")]
    Animator _animator = null;

    /// <summary>カメラで撮影した顔情報を受け取れるインターフェース</summary>
    IGetFaceData _faceData = null;

    [Header("ブレンドシェイプのIndex番号")]
    [SerializeField, Tooltip("口を閉じるブレンドシェイプの番号")]
    short _indexMouthClose = 0;

    [SerializeField, Tooltip("口を開けるブレンドシェイプの番号")]
    short _indexMouthOpen = 0;

    [SerializeField, Tooltip("口を狭めるブレンドシェイプの番号")]
    short _indexMouthNarrow = 0;

    [SerializeField, Tooltip("右口角をあげるブレンドシェイプの番号")]
    short _indexMouthRightCornerUp = 0;

    [SerializeField, Tooltip("右口角を下げるブレンドシェイプの番号")]
    short _indexMouthRightCornerDown = 0;

    [SerializeField, Tooltip("左口角をあげるブレンドシェイプの番号")]
    short _indexMouthLeftCornerUp = 0;

    [SerializeField, Tooltip("左口角を下げるブレンドシェイプの番号")]
    short _indexMouthLeftCornerDown = 0;

    [SerializeField, Tooltip("右目を閉じるブレンドシェイプの番号")]
    short _indexRightEyeClose = 0;

    [SerializeField, Tooltip("右目を開けるブレンドシェイプの番号")]
    short _indexRightEyeOpen = 0;

    [SerializeField, Tooltip("左目を閉じるブレンドシェイプの番号")]
    short _indexLeftEyeClose = 0;

    [SerializeField, Tooltip("左目を開けるブレンドシェイプの番号")]
    short _indexLeftEyeOpen = 0;

    [SerializeField, Tooltip("右眉を上げるブレンドシェイプの番号")]
    short _indexRightEyebrowUp = 0;

    [SerializeField, Tooltip("右眉を下げるブレンドシェイプの番号")]
    short _indexRightEyebrowDown = 0;

    [SerializeField, Tooltip("左眉を上げるブレンドシェイプの番号")]
    short _indexLeftEyebrowUp = 0;

    [SerializeField, Tooltip("左眉を下げるブレンドシェイプの番号")]
    short _indexLeftEyebrowDown = 0;


    #region ニュートラル値
    /// <summary>顔の元の位置</summary>
    Vector3 _neutralFacePos = Vector3.zero;

    /// <summary>顔位置に相対的な鼻の元の位置</summary>
    Vector3 _neutralNoseRelativePos = Vector3.zero;

    #endregion

    // Start is called before the first frame update
    void Start()
    {
        if (_faceDataObj) _faceData = _faceDataObj.GetComponent<IGetFaceData>();

        _neutralFacePos = _faceSkin.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if(_faceData is not null && _faceSkin is not null)
        {
            _animator.transform.position = _neutralFacePos + new Vector3(_faceData.FacePosDiff.x, -_faceData.FacePosDiff.y) * 0.001f;

            //口の開閉
            if(_faceData.MouthOpenDiff.y < 0f)
            {
                _faceSkin.SetBlendShapeWeight(_indexMouthClose, -_faceData.MouthOpenDiff.y);
            }
            else
            {
                _faceSkin.SetBlendShapeWeight(_indexMouthOpen, _faceData.MouthOpenDiff.y);
            }
            if(_faceData.MouthOpenDiff.x < 0)
            {
                _faceSkin.SetBlendShapeWeight(_indexMouthNarrow, -_faceData.MouthOpenDiff.x);
            }

            //右口角
            if (_faceData.MouthCornerUpDiff[0] > 0)
            {
                _faceSkin.SetBlendShapeWeight(_indexMouthRightCornerUp, _faceData.MouthCornerUpDiff[0] * 50f);
                _faceSkin.SetBlendShapeWeight(_indexMouthRightCornerDown, 0f);
            }
            else
            {
                _faceSkin.SetBlendShapeWeight(_indexMouthRightCornerUp, 0f);
                _faceSkin.SetBlendShapeWeight(_indexMouthRightCornerDown, -_faceData.MouthCornerUpDiff[0] * 50f);
            }

            //左口角
            if (_faceData.MouthCornerUpDiff[1] > 0)
            {
                _faceSkin.SetBlendShapeWeight(_indexMouthLeftCornerUp, _faceData.MouthCornerUpDiff[1] * 50f);
                _faceSkin.SetBlendShapeWeight(_indexMouthLeftCornerDown, 0f);
            }
            else
            {
                _faceSkin.SetBlendShapeWeight(_indexMouthLeftCornerUp, 0f);
                _faceSkin.SetBlendShapeWeight(_indexMouthLeftCornerDown, -_faceData.MouthCornerUpDiff[1] * 50f);
            }

            //右目
            if (_faceData.EyesOpenDiff[0] > 0)
            {
                _faceSkin.SetBlendShapeWeight(_indexRightEyeOpen, _faceData.EyesOpenDiff[0] * 50f);
                _faceSkin.SetBlendShapeWeight(_indexRightEyeClose, 0f);
            }
            else
            {
                _faceSkin.SetBlendShapeWeight(_indexRightEyeOpen, 0f);
                _faceSkin.SetBlendShapeWeight(_indexRightEyeClose, _faceData.EyesOpenDiff[0] * 50f);
            }

            //左目
            if (_faceData.EyesOpenDiff[1] > 0)
            {
                _faceSkin.SetBlendShapeWeight(_indexLeftEyeOpen, _faceData.EyesOpenDiff[1] * 50f);
                _faceSkin.SetBlendShapeWeight(_indexLeftEyeClose, 0f);
            }
            else
            {
                _faceSkin.SetBlendShapeWeight(_indexLeftEyeOpen, 0f);
                _faceSkin.SetBlendShapeWeight(_indexLeftEyeClose, _faceData.EyesOpenDiff[1] * 50f);
            }

            //右眉
            if (_faceData.EyebrowUpDiff[0] > 0)
            {
                _faceSkin.SetBlendShapeWeight(_indexRightEyebrowUp, _faceData.EyebrowUpDiff[0] * 50f);
                _faceSkin.SetBlendShapeWeight(_indexRightEyebrowDown, 0f);
            }
            else
            {
                _faceSkin.SetBlendShapeWeight(_indexRightEyebrowUp, 0f);
                _faceSkin.SetBlendShapeWeight(_indexRightEyebrowDown, -_faceData.EyebrowUpDiff[0] * 50f);
            }

            //左眉
            if (_faceData.EyebrowUpDiff[1] > 0)
            {
                _faceSkin.SetBlendShapeWeight(_indexLeftEyebrowUp, _faceData.EyebrowUpDiff[1] * 50f);
                _faceSkin.SetBlendShapeWeight(_indexLeftEyebrowDown, 0f);
            }
            else
            {
                _faceSkin.SetBlendShapeWeight(_indexLeftEyebrowUp, 0f);
                _faceSkin.SetBlendShapeWeight(_indexLeftEyebrowDown, -_faceData.EyebrowUpDiff[1] * 50f);
            }
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FaceBlendShape : MonoBehaviour
{
    [SerializeField, Tooltip("�J�����ŎB�e�������������GameObject")]
    GameObject _faceDataObj = null;

    [SerializeField, Tooltip("�����K�p������烌���_�[���[")]
    SkinnedMeshRenderer _faceSkin = null;

    [SerializeField, Tooltip("��𓮂����Ƃ���IK�ɂ��āA�^�[�Q�b�g�Ƃ���I�u�W�F�N�g")]
    GameObject _ikFacePosTarget = null;

    [SerializeField, Tooltip("�Ώۃ��f���̃A�j���[�^")]
    Animator _animator = null;

    /// <summary>�J�����ŎB�e����������󂯎���C���^�[�t�F�[�X</summary>
    IGetFaceData _faceData = null;

    [Header("�u�����h�V�F�C�v��Index�ԍ�")]
    [SerializeField, Tooltip("�������u�����h�V�F�C�v�̔ԍ�")]
    short _indexMouthClose = 0;

    [SerializeField, Tooltip("�������߂�u�����h�V�F�C�v�̔ԍ�")]
    short _indexMouthNarrow = 0;

    [SerializeField, Tooltip("�E���p��������u�����h�V�F�C�v�̔ԍ�")]
    short _indexMouthRightCornerUp = 0;

    [SerializeField, Tooltip("�E���p��������u�����h�V�F�C�v�̔ԍ�")]
    short _indexMouthRightCornerDown = 0;

    [SerializeField, Tooltip("�����p��������u�����h�V�F�C�v�̔ԍ�")]
    short _indexMouthLeftCornerUp = 0;

    [SerializeField, Tooltip("�����p��������u�����h�V�F�C�v�̔ԍ�")]
    short _indexMouthLeftCornerDown = 0;

    [SerializeField, Tooltip("�E�ڂ����u�����h�V�F�C�v�̔ԍ�")]
    short _indexRightEyeClose = 0;

    [SerializeField, Tooltip("�E�ڂ��J����u�����h�V�F�C�v�̔ԍ�")]
    short _indexRightEyeOpen = 0;

    [SerializeField, Tooltip("���ڂ����u�����h�V�F�C�v�̔ԍ�")]
    short _indexLeftEyeClose = 0;

    [SerializeField, Tooltip("���ڂ��J����u�����h�V�F�C�v�̔ԍ�")]
    short _indexLeftEyeOpen = 0;

    [SerializeField, Tooltip("�E�����グ��u�����h�V�F�C�v�̔ԍ�")]
    short _indexRightEyebrowUp = 0;

    [SerializeField, Tooltip("�E����������u�����h�V�F�C�v�̔ԍ�")]
    short _indexRightEyebrowDown = 0;

    [SerializeField, Tooltip("�������グ��u�����h�V�F�C�v�̔ԍ�")]
    short _indexLeftEyebrowUp = 0;

    [SerializeField, Tooltip("������������u�����h�V�F�C�v�̔ԍ�")]
    short _indexLeftEyebrowDown = 0;


    #region �j���[�g�����l
    /// <summary>��̌��̈ʒu</summary>
    Vector3 _neutralFacePos = Vector3.zero;

    /// <summary>��ʒu�ɑ��ΓI�ȕ@�̌��̈ʒu</summary>
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

            //���̊J��
            _faceSkin.SetBlendShapeWeight(_indexMouthClose, _faceData.MouthOpenDiff.y);
            _faceSkin.SetBlendShapeWeight(_indexMouthNarrow, _faceData.MouthOpenDiff.x);

            //�E���p
            if (_faceData.MouthCornerUpDiff[0] < 0)
            {
                _faceSkin.SetBlendShapeWeight(_indexMouthRightCornerUp, _faceData.MouthCornerUpDiff[0]);
                _faceSkin.SetBlendShapeWeight(_indexMouthRightCornerDown, 0f);
            }
            else
            {
                _faceSkin.SetBlendShapeWeight(_indexMouthRightCornerUp, 0f);
                _faceSkin.SetBlendShapeWeight(_indexMouthRightCornerDown, _faceData.MouthCornerUpDiff[0]);
            }
        }
    }
}

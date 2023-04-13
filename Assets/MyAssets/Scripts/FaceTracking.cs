using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using OpenCvSharp;
using OpenCvSharp.Demo;

public class FaceTracking : WebCamera, IGetFaceData
{
    [SerializeField, Tooltip("��̎��ʕ��@�o�C�i��")]
    TextAsset _faces;
    [SerializeField, Tooltip("�ڂ̎��ʕ��@�o�C�i��")]
    TextAsset _eyes;
    [SerializeField, Tooltip("���̑��A��̓��������ʃo�C�i��")]
    TextAsset _shapes;


    /// <summary>�摜���H����</summary>
    FaceProcessorLive<WebCamTexture> _processor;


    #region ����
    /// <summary>true : ���̃t���[���͊�l���L�^����</summary>
    bool _doRecord = true;

    /// <summary>��̍\���v�f�̍��W�̊�l</summary>
    OpenCvSharp.Rect _faceRectBase = OpenCvSharp.Rect.Empty;

    /// <summary>�@�̍\���v�f�̍��W�̊�l</summary>
    Point[] _noseMarksBase = null;

    /// <summary>�O�O���̍\���v�f�̍��W�̊�l</summary>
    Point[] _lipOutsideMarksBase = null;

    /// <summary>�O�����̍\���v�f�̍��W�̊�l</summary>
    Point[] _lipInsideMarksBase = null;

    /// <summary>�E�ڂ̍\���v�f�̍��W�̊�l</summary>
    Point[] _rightEyeMarksBase = null;

    /// <summary>�E���̍\���v�f�̍��W�̊�l</summary>
    Point[] _rightEyeblowMarksBase = null;

    /// <summary>���ڂ̍\���v�f�̍��W�̊�l</summary>
    Point[] _leftEyeMarksBase = null;

    /// <summary>�����̍\���v�f�̍��W�̊�l</summary>
    Point[] _leftEyeblowMarksBase = null;
    #endregion


    #region �B�e������̃Y�����
    /// <summary>��p�[�c�̊�ʒu����̃Y��</summary>
    Vector2 _facePosDiff = Vector2.zero;

    /// <summary>�@�p�[�c�̊�ʒu����̃Y��</summary>
    Vector2 _nosePosDiff = Vector2.zero;

    /// <summary>���p�[�c�̊J���ʂ̃Y��</summary>
    Vector2 _mouthOpenDiff = Vector2.zero;

    /// <summary>���p�[�c�E���p�̏㉺�ʂ̃Y�� [0]:�E [1]:�� ��:�� ��:��</summary>
    float[] _mouthCornerUpDiff = null;

    /// <summary>�ڃp�[�c�̊J��ʂ̃Y��  [0]:�E [1]:�� ��:�J ��:��</summary>
    float[] _eyesOpenDiff = null;

    /// <summary>���p�[�c�̏㉺�ʂ̃Y��  [0,0]:�E�̓��� [1,0]:���̊O�� [1,1]:�E�̍� [1,1]:���̍� ��:�� ��:��</summary>
    float[] _eyebrowUpDiff = null;


    public Vector2 FacePosDiff => _facePosDiff;

    public Vector2 NosePosDiff => _nosePosDiff;

    public Vector2 MouthOpenDiff => _mouthOpenDiff;

    public float[] MouthCornerUpDiff => _mouthCornerUpDiff;

    public float[] EyesOpenDiff => _eyesOpenDiff;

    public float[] EyebrowUpDiff => _eyebrowUpDiff;
    #endregion



    protected override void Awake()
    {
        base.Awake();

        //�O�ʃJ�������g�p
        base.forceFrontalCamera = true;

        _processor = new FaceProcessorLive<WebCamTexture>();
        _processor.Initialize(_faces.text, _eyes.text, _shapes.bytes);

        /* ����l�p�`�ň͂��A�ځE�@�E����T���f���̈��艻��}�� */
        // ���艻������L����
        _processor.DataStabilizer.Enabled = true; 
        // �s�N�Z�����x����臒l�ݒ�
        _processor.DataStabilizer.Threshold = 2.0;
        //���肵���f�[�^���v�Z���邽�߂ɕK�v�ȃT���v����
        _processor.DataStabilizer.SamplesCount = 2;

        /* �����������̂��߂̍H�v */
        //�摜�̒��ӂ����̃s�N�Z�����܂łɏk��
        _processor.Performance.Downscale = 256; 
        //����������t���[�������̐��l�����X�L�b�v
        _processor.Performance.SkipRate = 0;

        _mouthCornerUpDiff = new float[2];
        _eyesOpenDiff = new float[2];
        _eyebrowUpDiff = new float[2];
    }

    /// <summary>��F���������̂��f���ɔ��f����</summary>
    protected override bool ProcessTexture(WebCamTexture input, ref Texture2D output)
    {
        // �������o
        _processor.ProcessTexture(input, TextureParameters);

        // �������������Ɉ������
        _processor.MarkDetected();

        // �o�͂��L���ȃe�N�X�`���̏ꍇ�A���̃o�b�t�@�͍ė��p�A�����łȂ��ꍇ�͍č쐬
        output = OpenCvSharp.Unity.MatToTexture(_processor.Image, output);




        //��̑��݂��擾�ł��Ă���Ή��
        if(_processor.Faces.Count > 0 && _processor.Faces[0].Region.X > 0)
        {
            //��ʒu���L��
            if (_doRecord)
            {
                bool isNotRecord = false;

                DetectedObject eyebrowCash = null;
                DetectedObject eyeCash = null;
                DetectedObject lipCash = null;

                //��̈ʒu�����i�[
                _faceRectBase = _processor.Faces[0].Region;
                foreach (DetectedObject part in _processor.Faces[0].Elements)
                {
                    if (part is null || part.Marks is null)
                    {
                        isNotRecord = true;
                    }
                    else
                    {
                        switch (part.Name)
                        {
                            //���p�[�c��Rect��X���W���獶�E�����ʂ��Ċi�[
                            case "Eyebrow":

                                if (eyebrowCash is null)
                                {
                                    eyebrowCash = part;
                                }
                                else
                                {
                                    if (part.Region.X < eyebrowCash.Region.X)
                                    {
                                        _rightEyeblowMarksBase = part.Marks;
                                        _leftEyeblowMarksBase = eyebrowCash.Marks;
                                    }
                                    else
                                    {
                                        _leftEyeblowMarksBase = part.Marks;
                                        _rightEyeblowMarksBase = eyebrowCash.Marks;
                                    }
                                }

                                break;

                            //�ڃp�[�c��Rect��X���W���獶�E�����ʂ��Ċi�[
                            case "Eye":

                                if (eyeCash is null)
                                {
                                    eyeCash = part;
                                }
                                else
                                {
                                    if (part.Region.X < eyeCash.Region.X)
                                    {
                                        _rightEyeMarksBase = part.Marks;
                                        _leftEyeMarksBase = eyeCash.Marks;
                                    }
                                    else
                                    {
                                        _leftEyeMarksBase = part.Marks;
                                        _rightEyeMarksBase = eyeCash.Marks;
                                    }
                                }
                                break;

                            //�@�̃p�[�c����荞��
                            case "Nose":

                                _noseMarksBase = part.Marks;
                                break;

                            //�O�̃p�[�c�𒸓_�����r���Ċi�[
                            case "Lip":

                                if (lipCash is null)
                                {
                                    lipCash = part;
                                }
                                else
                                {
                                    if (part.Marks.Length < lipCash.Marks.Length)
                                    {
                                        _lipInsideMarksBase = part.Marks;
                                        _lipOutsideMarksBase = lipCash.Marks;
                                    }
                                    else
                                    {
                                        _lipOutsideMarksBase = part.Marks;
                                        _lipInsideMarksBase = lipCash.Marks;
                                    }
                                }

                                break;
                        }
                    }
                }

                _doRecord = isNotRecord;
            }
            //��̈ʒu����ۑ��������̂Ɠ˂����킹��
            else
            {
                DetectedObject eyebrowCash = null;
                DetectedObject eyeCash = null;
                DetectedObject lipCash = null;

                Point point = _processor.Faces[0].Region.Center - _faceRectBase.Center;
                _facePosDiff = new Vector2(point.X, point.Y);

                foreach (DetectedObject part in _processor.Faces[0].Elements)
                {
                    if (part.Marks is null || part.Marks.Length < 1) continue; 

                    switch (part.Name)
                    {
                        //���p�[�c�̊�ʒu��r
                        case "Eyebrow":

                            if (eyebrowCash is null)
                            {
                                eyebrowCash = part;
                            }
                            else
                            {
                                if (part.Region.X < eyebrowCash.Region.X)
                                {
                                    _eyebrowUpDiff[0] = EyebrowUpCalculation(part.Marks, _rightEyeblowMarksBase);
                                    _eyebrowUpDiff[1] = EyebrowUpCalculation(eyebrowCash.Marks, _leftEyeblowMarksBase);
                                }
                                else
                                {
                                    _eyebrowUpDiff[1] = EyebrowUpCalculation(part.Marks, _leftEyeblowMarksBase);
                                    _eyebrowUpDiff[0] = EyebrowUpCalculation(eyebrowCash.Marks, _rightEyeblowMarksBase);
                                }
                            }

                            break;

                        //�ڃp�[�c�̊�ʒu��r
                        case "Eye":

                            if (eyeCash is null)
                            {
                                eyeCash = part;
                            }
                            else
                            {
                                //part���E�� eyeCash������
                                if (part.Region.X < eyeCash.Region.X)
                                {
                                    _eyesOpenDiff[0] = EyeOpenCalculation(part.Marks, _rightEyeMarksBase);
                                    _eyesOpenDiff[1] = EyeOpenCalculation(eyeCash.Marks, _leftEyeMarksBase);
                                }
                                //part������ eyeCash���E��
                                else
                                {
                                    _eyesOpenDiff[1] = EyeOpenCalculation(part.Marks, _leftEyeMarksBase);
                                    _eyesOpenDiff[0] = EyeOpenCalculation(eyeCash.Marks, _rightEyeMarksBase);
                                }
                            }
                            break;

                        //�@�p�[�c�̊�ʒu��r
                        case "Nose":

                            point = GetRegion(part.Marks).Center - GetRegion(_noseMarksBase).Center;
                            _nosePosDiff = new Vector2(point.X, point.Y);

                            break;

                        //�O�p�[�c�̊�ʒu��r
                        case "Lip":

                            if (lipCash is null)
                            {
                                lipCash = part;
                            }
                            else
                            {
                                //part������ lipCash���O��
                                if (part.Marks.Length < lipCash.Marks.Length)
                                {
                                    OpenCvSharp.Rect mouthRect = GetRegion(_lipInsideMarksBase);
                                    _mouthOpenDiff = new Vector2(part.Region.Width - mouthRect.Width, part.Region.Height - mouthRect.Height);

                                    Point[] cashe = part.Marks.OrderBy(p => p.X).ToArray();
                                    Point minX = cashe.First();
                                    Point maxX = cashe.Last();
                                    cashe = _lipInsideMarksBase.OrderBy(p => p.X).ToArray();
                                    Point baseMinX = cashe.First();
                                    Point baseMaxX = cashe.Last();

                                    _mouthCornerUpDiff[0] = (minX.Y - baseMinX.Y) / (mouthRect.Height / 2f);
                                    _mouthCornerUpDiff[1] = (maxX.Y - baseMaxX.Y) / (mouthRect.Height / 2f);
                                }
                                //part���O�� lipCash������
                                else
                                {
                                    OpenCvSharp.Rect mouthRect = GetRegion(_lipInsideMarksBase);
                                    _mouthOpenDiff = new Vector2(lipCash.Region.Width - mouthRect.Width, lipCash.Region.Height - mouthRect.Height);

                                    Point[] cashe = lipCash.Marks.OrderBy(p => p.X).ToArray();
                                    Point minX = cashe.First();
                                    Point maxX = cashe.Last();
                                    cashe = _lipInsideMarksBase.OrderBy(p => p.X).ToArray();
                                    Point baseMinX = cashe.First();
                                    Point baseMaxX = cashe.Last();

                                    _mouthCornerUpDiff[0] = (minX.Y - baseMinX.Y) / (mouthRect.Height / 2f);
                                    _mouthCornerUpDiff[1] = (maxX.Y - baseMaxX.Y) / (mouthRect.Height / 2f);
                                }
                            }

                            break;
                    }
                }
            }
        }

        return true;
    }

    /// <summary>�蓮��Mark�̒��_��񂩂�Region�̃G���A�����X�V</summary>
    /// <param name="points">Mark�̒��_���</param>
    /// <returns>Region�G���A</returns>
    OpenCvSharp.Rect GetRegion(Point[] points)
    {
        int minX = points.Min(mk => mk.X);
        int maxX = points.Max(mk => mk.X);
        int minY = points.Min(mk => mk.Y);
        int maxY = points.Max(mk => mk.Y);

        return new OpenCvSharp.Rect(minX, minY, maxX - minX, maxY - minY);
    }

    /// <summary>���p�[�c�������S���W����A���������Ă���ʂ𑪒�</summary>
    /// <param name="points">�ڃp�[�c���\��������W</param>
    /// <param name="basePoints">�ڃp�[�c���\�������̍��W</param>
    /// <returns>������ɂ����銄�� 1�ȏ�:�グ 1:�f�t�H���g 1����:����</returns>
    float EyebrowUpCalculation(Point[] points, Point[] basePoints)
    {
        OpenCvSharp.Rect rect = GetRegion(points);
        OpenCvSharp.Rect baseRect = GetRegion(basePoints);

        float returnal = (rect.Y - baseRect.Y) / (baseRect.Height / 2f);

        return returnal;
    }


    /// <summary>�ڃp�[�c�������S���W����A�ڂ��J���Ă���ʂ𑪒�</summary>
    /// <param name="points">�ڃp�[�c���\��������W</param>
    /// <param name="basePoints">�ڃp�[�c���\�������̍��W</param>
    /// <returns>�J�ኄ�� 1�ȏ�:���J�� 1:�f�t�H���g 0:��</returns>
    float EyeOpenCalculation(Point[] points, Point[] basePoints)
    {
        OpenCvSharp.Rect rect = GetRegion(points);
        OpenCvSharp.Rect baseRect = GetRegion(basePoints);

        float returnal = (rect.Height / (float)baseRect.Height) - 0.5f;

        return returnal; 
    }

    /// <summary>��ʒu�L�^�����s</summary>
    public void RecordBasePos()
    {
        _doRecord = true;
    }
}

/// <summary>����̒񋟂����s�킹��C���^�[�t�F�[�X</summary>
public interface IGetFaceData
{
    /// <summary>��̈ʒu�̍���</summary>
    Vector2 FacePosDiff { get; }

    /// <summary>�@�̈ʒu�̍���</summary>
    Vector2 NosePosDiff { get; }

    /// <summary></summary>
    Vector2 MouthOpenDiff { get;}

    /// <summary></summary>
    float[] MouthCornerUpDiff { get; }

    /// <summary></summary>
    float[] EyesOpenDiff { get;}

    /// <summary></summary>
    float[] EyebrowUpDiff { get; }
}

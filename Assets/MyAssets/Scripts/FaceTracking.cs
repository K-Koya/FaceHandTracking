using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using OpenCvSharp;
using OpenCvSharp.Demo;

public class FaceTracking : WebCamera, IGetFaceData
{
    [SerializeField, Tooltip("顔の識別方法バイナリ")]
    TextAsset _faces;
    [SerializeField, Tooltip("目の識別方法バイナリ")]
    TextAsset _eyes;
    [SerializeField, Tooltip("その他、顔の特徴部識別バイナリ")]
    TextAsset _shapes;


    /// <summary>画像加工処理</summary>
    FaceProcessorLive<WebCamTexture> _processor;


    #region 基準情報
    /// <summary>true : そのフレームは基準値を記録する</summary>
    bool _doRecord = true;

    /// <summary>顔の構成要素の座標の基準値</summary>
    OpenCvSharp.Rect _faceRectBase = OpenCvSharp.Rect.Empty;

    /// <summary>鼻の構成要素の座標の基準値</summary>
    Point[] _noseMarksBase = null;

    /// <summary>唇外側の構成要素の座標の基準値</summary>
    Point[] _lipOutsideMarksBase = null;

    /// <summary>唇内側の構成要素の座標の基準値</summary>
    Point[] _lipInsideMarksBase = null;

    /// <summary>右目の構成要素の座標の基準値</summary>
    Point[] _rightEyeMarksBase = null;

    /// <summary>右眉の構成要素の座標の基準値</summary>
    Point[] _rightEyeblowMarksBase = null;

    /// <summary>左目の構成要素の座標の基準値</summary>
    Point[] _leftEyeMarksBase = null;

    /// <summary>左眉の構成要素の座標の基準値</summary>
    Point[] _leftEyeblowMarksBase = null;
    #endregion


    #region 撮影した顔のズレ情報
    /// <summary>顔パーツの基準位置からのズレ</summary>
    Vector2 _facePosDiff = Vector2.zero;

    /// <summary>鼻パーツの基準位置からのズレ</summary>
    Vector2 _nosePosDiff = Vector2.zero;

    /// <summary>口パーツの開口量のズレ</summary>
    Vector2 _mouthOpenDiff = Vector2.zero;

    /// <summary>口パーツ・口角の上下量のズレ [0]:右 [1]:左 正:上 負:下</summary>
    float[] _mouthCornerUpDiff = null;

    /// <summary>目パーツの開眼量のズレ  [0]:右 [1]:左 正:開 負:閉</summary>
    float[] _eyesOpenDiff = null;

    /// <summary>眉パーツの上下量のズレ  [0,0]:右の内側 [1,0]:左の外側 [1,1]:右の左 [1,1]:左の左 正:上 負:下</summary>
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

        //前面カメラを使用
        base.forceFrontalCamera = true;

        _processor = new FaceProcessorLive<WebCamTexture>();
        _processor.Initialize(_faces.text, _eyes.text, _shapes.bytes);

        /* 顔を四角形で囲う、目・鼻・口を探す映像の安定化を図る */
        // 安定化処理を有効化
        _processor.DataStabilizer.Enabled = true; 
        // ピクセルレベルの閾値設定
        _processor.DataStabilizer.Threshold = 2.0;
        //安定したデータを計算するために必要なサンプル数
        _processor.DataStabilizer.SamplesCount = 2;

        /* 高速化処理のための工夫 */
        //画像の長辺をこのピクセル数までに縮小
        _processor.Performance.Downscale = 256; 
        //処理を入れるフレームをこの数値だけスキップ
        _processor.Performance.SkipRate = 0;

        _mouthCornerUpDiff = new float[2];
        _eyesOpenDiff = new float[2];
        _eyebrowUpDiff = new float[2];
    }

    /// <summary>顔認識したものを映像に反映する</summary>
    protected override bool ProcessTexture(WebCamTexture input, ref Texture2D output)
    {
        // 特徴検出
        _processor.ProcessTexture(input, TextureParameters);

        // 発見した特徴に印をつける
        _processor.MarkDetected();

        // 出力が有効なテクスチャの場合、そのバッファは再利用、そうでない場合は再作成
        output = OpenCvSharp.Unity.MatToTexture(_processor.Image, output);




        //顔の存在を取得できていれば解析
        if(_processor.Faces.Count > 0 && _processor.Faces[0].Region.X > 0)
        {
            //基準位置を記憶
            if (_doRecord)
            {
                bool isNotRecord = false;

                DetectedObject eyebrowCash = null;
                DetectedObject eyeCash = null;
                DetectedObject lipCash = null;

                //顔の位置情報を格納
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
                            //眉パーツはRectのX座標から左右を識別して格納
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

                            //目パーツもRectのX座標から左右を識別して格納
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

                            //鼻のパーツを取り込み
                            case "Nose":

                                _noseMarksBase = part.Marks;
                                break;

                            //唇のパーツを頂点数を比較して格納
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
            //顔の位置情報を保存したものと突き合わせる
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
                        //眉パーツの基準位置比較
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

                        //目パーツの基準位置比較
                        case "Eye":

                            if (eyeCash is null)
                            {
                                eyeCash = part;
                            }
                            else
                            {
                                //partが右目 eyeCashが左目
                                if (part.Region.X < eyeCash.Region.X)
                                {
                                    _eyesOpenDiff[0] = EyeOpenCalculation(part.Marks, _rightEyeMarksBase);
                                    _eyesOpenDiff[1] = EyeOpenCalculation(eyeCash.Marks, _leftEyeMarksBase);
                                }
                                //partが左目 eyeCashが右目
                                else
                                {
                                    _eyesOpenDiff[1] = EyeOpenCalculation(part.Marks, _leftEyeMarksBase);
                                    _eyesOpenDiff[0] = EyeOpenCalculation(eyeCash.Marks, _rightEyeMarksBase);
                                }
                            }
                            break;

                        //鼻パーツの基準位置比較
                        case "Nose":

                            point = GetRegion(part.Marks).Center - GetRegion(_noseMarksBase).Center;
                            _nosePosDiff = new Vector2(point.X, point.Y);

                            break;

                        //唇パーツの基準位置比較
                        case "Lip":

                            if (lipCash is null)
                            {
                                lipCash = part;
                            }
                            else
                            {
                                //partが内側 lipCashが外側
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
                                //partが外側 lipCashが内側
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

    /// <summary>手動でMarkの頂点情報からRegionのエリア情報を更新</summary>
    /// <param name="points">Markの頂点情報</param>
    /// <returns>Regionエリア</returns>
    OpenCvSharp.Rect GetRegion(Point[] points)
    {
        int minX = points.Min(mk => mk.X);
        int maxX = points.Max(mk => mk.X);
        int minY = points.Min(mk => mk.Y);
        int maxY = points.Max(mk => mk.Y);

        return new OpenCvSharp.Rect(minX, minY, maxX - minX, maxY - minY);
    }

    /// <summary>眉パーツを示す全座標から、眉をあげている量を測定</summary>
    /// <param name="points">目パーツを構成する座標</param>
    /// <param name="basePoints">目パーツを構成する基準の座標</param>
    /// <returns>眉を上にあげる割合 1以上:上げ 1:デフォルト 1未満:下げ</returns>
    float EyebrowUpCalculation(Point[] points, Point[] basePoints)
    {
        OpenCvSharp.Rect rect = GetRegion(points);
        OpenCvSharp.Rect baseRect = GetRegion(basePoints);

        float returnal = (rect.Y - baseRect.Y) / (baseRect.Height / 2f);

        return returnal;
    }


    /// <summary>目パーツを示す全座標から、目を開けている量を測定</summary>
    /// <param name="points">目パーツを構成する座標</param>
    /// <param name="basePoints">目パーツを構成する基準の座標</param>
    /// <returns>開眼割合 1以上:見開き 1:デフォルト 0:閉</returns>
    float EyeOpenCalculation(Point[] points, Point[] basePoints)
    {
        OpenCvSharp.Rect rect = GetRegion(points);
        OpenCvSharp.Rect baseRect = GetRegion(basePoints);

        float returnal = (rect.Height / (float)baseRect.Height) - 0.5f;

        return returnal; 
    }

    /// <summary>基準位置記録を実行</summary>
    public void RecordBasePos()
    {
        _doRecord = true;
    }
}

/// <summary>顔情報の提供だけ行わせるインターフェース</summary>
public interface IGetFaceData
{
    /// <summary>顔の位置の差異</summary>
    Vector2 FacePosDiff { get; }

    /// <summary>鼻の位置の差異</summary>
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

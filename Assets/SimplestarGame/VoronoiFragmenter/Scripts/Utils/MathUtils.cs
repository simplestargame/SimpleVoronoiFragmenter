using System.Collections.Generic;
using UnityEngine;

namespace SimplestarGame
{
    public class MathUtils
    {
        /// <summary>
        /// 多角形が凸であるかどうか
        /// </summary>
        /// <param name="vertices">多角形の頂点リスト（時計回りでソート済み）</param>
        /// <returns>多角形が凸である場合真</returns>
        public static bool IsConvex(List<Vector2> vertices)
        {
            // 頂点の数が3未満の場合は凸とする
            if (vertices.Count < 3)
            {
                return true;
            }

            bool sign = false;
            int n = vertices.Count;
            for (int i = 0; i < n; i++)
            {
                Vector2 v1 = vertices[i];
                Vector2 v2 = vertices[(i + 1) % n];
                Vector2 v3 = vertices[(i + 2) % n];
                float cross = CrossProduct(v1, v2, v3);
                if (i == 0)
                {
                    sign = cross > 0;
                }
                else if (sign != (cross > 0) && Mathf.Abs(cross) > 0.001f)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 3つの 2D ベクトルの外積
        /// </summary>
        /// <returns>外積</returns>
        public static float CrossProduct(Vector2 v1, Vector2 v2, Vector2 v3)
        {
            float x1 = v2.x - v1.x;
            float y1 = v2.y - v1.y;
            float x2 = v3.x - v2.x;
            float y2 = v3.y - v2.y;
            return x1 * y2 - x2 * y1;
        }

        /// <summary>
        /// 多角形の面積を計算
        /// </summary>
        /// <param name="vertices">多角形の構成点</param>
        /// <returns>面積</returns>
        public static float CalculateArea(List<Vector2> vertices)
        {
            float area = 0;
            for (int i = 0; i < vertices.Count; i++)
            {
                int j = (i + 1) % vertices.Count;

                area += vertices[i].x * vertices[j].y;
                area -= vertices[j].x * vertices[i].y;
            }
            area /= 2.0f;
            area = Mathf.Abs(area);
            return area;
        }

        /// <summary>
        /// 頂点が矩形の内側にあるか判定
        /// </summary>
        /// <param name="min">矩形の最小座標</param>
        /// <param name="max">矩形の最大座標</param>
        /// <param name="point">頂点</param>
        /// <returns>矩形の内側にある場合真</returns>
        public static bool IsInsideRect(Vector2 min, Vector2 max, Vector2 point)
        {
            if (min.x >= point.x)
            {
                return false;
            }
            if (min.y >= point.y)
            {
                return false;
            }
            if (max.x <= point.x)
            {
                return false;
            }
            if (max.y <= point.y)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 点が多角形のいずれかの辺に交わる場合、その交点を計算
        /// </summary>
        /// <param name="vertices">多角形を構成する頂点群</param>
        /// <param name="point">点</param>
        /// <returns>二つの線分が交わる場合に、その交点（交わらない場合は Zero）</returns>
        public static Vector2 GetIntersectionOnPolygonEdge(List<Vector2> vertices, Vector2 line1Start, Vector2 line1End)
        {
            Vector2 intersection = Vector2.zero;
            for (int i = 0; i < vertices.Count; i++)
            {
                int j = (i + 1) % vertices.Count;
                intersection = GetIntersection(line1Start, line1End, vertices[i], vertices[j]);
                if (Vector2.zero != intersection)
                {
                    break;
                }
            }
            return intersection;
        }

        /// <summary>
        /// 二つの線分が交わる場合に、その交点を計算
        /// </summary>
        /// <param name="line1Start">線分１の開始点</param>
        /// <param name="line1End">線分１の終了点</param>
        /// <param name="line2Start">線分２の開始点</param>
        /// <param name="line2End">線分２の終了点</param>
        /// <returns>二つの線分が交わる場合に、その交点（交わらない場合は Zero）</returns>
        public static Vector2 GetIntersection(Vector2 line1Start, Vector2 line1End, Vector2 line2Start, Vector2 line2End)
        {
            Vector2 intersection = Vector2.zero;

            Vector2 line1 = line1End - line1Start;
            Vector2 line2 = line2End - line2Start;

            float cross = (line1.x * line2.y) - (line1.y * line2.x);

            if (Mathf.Approximately(cross, 0))
            {
                return intersection;
            }

            Vector2 distance = line2Start - line1Start;
            float distanceRatio = ((distance.x * line2.y) - (distance.y * line2.x)) / cross;
            intersection = line1Start + (distanceRatio * line1);

            if (intersection.x < Mathf.Min(line1Start.x, line1End.x) ||
                intersection.x > Mathf.Max(line1Start.x, line1End.x) ||
                intersection.x < Mathf.Min(line2Start.x, line2End.x) ||
                intersection.x > Mathf.Max(line2Start.x, line2End.x) ||
                intersection.y < Mathf.Min(line1Start.y, line1End.y) ||
                intersection.y > Mathf.Max(line1Start.y, line1End.y) ||
                intersection.y < Mathf.Min(line2Start.y, line2End.y) ||
                intersection.y > Mathf.Max(line2Start.y, line2End.y))
            {
                intersection = Vector2.zero;
            }

            return intersection;
        }

        /// <summary>
        /// 点が多角形の外側にあるか判断（角度を符号つきで計算すると外部にあるときは角度同士がキャンセルしあって和は0になる）
        /// </summary>
        /// <param name="vertices">多角形を構成する頂点群</param>
        /// <param name="point">点</param>
        /// <returns>多角形の外側にある場合に真</returns>
        public static bool IsOutsidePolygon(List<Vector2> vertices, Vector2 point)
        {
            float totalAngle = 0f;
            for (int i = 0; i < vertices.Count; i++)
            {
                int j = (i + 1) % vertices.Count;
                Vector2 v1 = vertices[i];
                Vector2 v2 = vertices[j];
                float angle = CalculateDigree(v1, v2, point);
                totalAngle += angle;
            }
            return Mathf.Abs(totalAngle) < 0.01f;
        }

        /// <summary>
        /// 点から見た、二つの点とのなす角を計算（符号付き）
        /// </summary>
        /// <param name="v1">二点のうち点１</param>
        /// <param name="v2">二点のうち点２</param>
        /// <param name="point">点</param>
        /// <returns>成す角</returns>
        public static float CalculateDigree(Vector2 v1, Vector2 v2, Vector2 point)
        {
            Vector2 vectorPA = v1 - point;
            Vector2 vectorPB = v2 - point;

            // ベクトルPA、PBの角度を計算
            return Vector2.SignedAngle(vectorPA, vectorPB);
        }

        /// <summary>
        /// 点から辺に垂線を引いた距離を計算
        /// </summary>
        /// <param name="v1">辺上点１</param>
        /// <param name="v2">辺上点２</param>
        /// <param name="point">点</param>
        /// <returns>点から辺に垂線を引いた距離</returns>
        public static float DistancePointToLine(Vector2 v1, Vector2 v2, Vector2 point)
        {
            float x1 = v1.x;
            float y1 = v1.y;
            float x2 = v2.x;
            float y2 = v2.y;
            float x3 = point.x;
            float y3 = point.y;

            float numerator = Mathf.Abs((y2 - y1) * x3 - (x2 - x1) * y3 + x2 * y1 - y2 * x1);
            float denominator = Mathf.Sqrt(Mathf.Pow(y2 - y1, 2) + Mathf.Pow(x2 - x1, 2));
            return numerator / denominator;
        }

        /// <summary>
        /// 多角形の周囲の距離を計算
        /// </summary>
        /// <param name="vertices">多角形を構成する頂点群</param>
        /// <returns>多角形の周囲の距離</returns>
        public static float CalculatePerimeter(List<Vector2> vertices)
        {
            float perimeter = 0f;
            for (int i = 0; i < vertices.Count; i++)
            {
                int j = (i + 1) % vertices.Count;
                Vector2 v1 = vertices[i];
                Vector2 v2 = vertices[j];
                float edgeLength = Vector2.Distance(v1, v2);
                perimeter += edgeLength;
            }
            return perimeter;
        }

        /// <summary>
        /// 頂点群を与えると反時計回りの順序にソート
        /// </summary>
        /// <param name="vertices">頂点群</param>
        /// <returns>反時計回りにソートした頂点群</returns>
        public static List<Vector2> SortVerticesCounterClockwise(List<Vector2> vertices)
        {
            // 頂点の中心座標を計算する
            Vector2 center = CalculateCenter(vertices);

            // 頂点を中心座標からの角度でソートする
            List<Vector2> sortedVertices = new List<Vector2>(vertices);
            sortedVertices.Sort((v1, v2) =>
            {
                float angle1 = Mathf.Atan2(v1.y - center.y, v1.x - center.x);
                float angle2 = Mathf.Atan2(v2.y - center.y, v2.x - center.x);
                return angle1.CompareTo(angle2);
            });

            return sortedVertices;
        }

        /// <summary>
        /// 頂点群の中心座標を計算
        /// </summary>
        /// <param name="vertices">頂点群</param>
        /// <returns>中心座標</returns>
        public static Vector2 CalculateCenter(List<Vector2> vertices)
        {
            Vector2 center = Vector2.zero;
            foreach (Vector2 vertex in vertices)
            {
                center += vertex;
            }
            center /= vertices.Count;
            return center;
        }

        /// <summary>
        /// 2点が十分近いか判定
        /// </summary>
        public static bool AreVectorsEqual(Vector2 vec1, Vector2 vec2, float threshold = 0.0001f)
        {
            return Vector2.Distance(vec1, vec2) < threshold;
        }

        /// <summary>
        /// 平面と線分の交点を返す
        /// </summary>
        /// <param name="planePoint">平面上の任意の点</param>
        /// <param name="planeNormal">平面の法線ベクトル（非正規化受け入れます）</param>
        /// <param name="linePoint">線分の起点</param>
        /// <param name="lineDirection">線分の方向ベクトル（非正規化受け入れます）</param>
        /// <param name="length">線分の長さ</param>
        /// <returns>交点（交わらない場合は線分の起点をそのまま返す）</returns>
        public static Vector3 PlaneIntersection(Vector3 planePoint, Vector3 planeNormal, Vector3 linePoint, Vector3 lineDirection, float length)
        {
            // 平面と線分が交わるかどうか判定する
            float dot = Vector3.Dot(planeNormal, lineDirection);
            if (dot == 0)
            {
                // 平行な場合は交わらないが、そのときは線分の起点をそのまま返す
                return linePoint;
            }
            else
            {
                // 媒介変数tを求める
                float t = Vector3.Dot(planeNormal, planePoint - linePoint) / dot;
                // tが正であれば交わる
                if (t > 0)
                {
                    // 線分上にあるかどうか判定する（必要なら）
                    if (t <= length)
                    {
                        // 線分上にある場合

                        // 交点を求める
                        Vector3 intersection = linePoint + t * lineDirection;
                        return intersection;
                    }
                    else
                    {
                        // 無限遠直線上にある場合（必要なら）、そのときは線分の起点をそのまま返す
                        return linePoint;
                    }
                }
                else
                {
                    // 負であれば交わらないが、そのときは線分の起点をそのまま返す
                    return linePoint;
                }
            }
        }
    }
}
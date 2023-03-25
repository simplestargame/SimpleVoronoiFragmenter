using csDelaunay;
using System.Collections.Generic;
using UnityEngine;

namespace SimplestarGame
{
    public class VoronoiFragmenter : MonoBehaviour
    {
        [SerializeField] MeshFilter fragmentPrefab;
        [SerializeField] int numberOfPoints = 50;
        [SerializeField] float scaleRadius = 0.6f;
        [SerializeField] float minFragmentSize = 0.1f;
        [SerializeField] float remainingTime = 5f;
        [SerializeField] float fadeDuration = 5f;
        [SerializeField] AnimationCurve fadeCurve;

        internal void Fragment(RaycastHit hit)
        {
            if (!this.TryGetComponent(out MeshFilter initMeshFilter))
            {
                Debug.LogWarning("破壊するターゲットの MeshFilter がアタッチされていません");
                return;
            }
            var scale = this.transform.localScale;
            var initVerts = initMeshFilter.sharedMesh.vertices;
            var initNormals = initMeshFilter.sharedMesh.normals;
            var initUVs = initMeshFilter.sharedMesh.uv;
            var resizedVerts = new Vector3[initVerts.Length];
            for (int i = 0; i < initVerts.Length; i++)
            {
                resizedVerts[i] = new Vector3(initVerts[i].x * scale.x, initVerts[i].y * scale.y, initVerts[i].z * scale.z);
            }            
            List<Vector2> uniqueVerts = new List<Vector2>();
            Vector2 uniqueVertsCenter = Vector2.zero;
            Vector2 fwdBk = uniqueVertsCenter;
            var initUVMin = new Vector2(1.0f, 1.0f);
            Vector2[] uvBoundsMin = new Vector2[6] { initUVMin, initUVMin, initUVMin, initUVMin, initUVMin, initUVMin };
            var initUVMax = new Vector2(0.0f, 0.0f);
            Vector2[] uvBoundsMax = new Vector2[6] { initUVMax, initUVMax, initUVMax, initUVMax, initUVMax, initUVMax };
            for (int i = 0; i < resizedVerts.Length; i++)
            {
                Vector2 dotPoint = resizedVerts[i];
                float depth = resizedVerts[i].z;
                fwdBk.x = Mathf.Min(fwdBk.x, depth);
                fwdBk.y = Mathf.Max(fwdBk.y, depth);
                var n = initNormals[i];
                int faceIdx = (int)this.GetNormalFaceIndex(n);                
                uvBoundsMin[faceIdx].x = Mathf.Min(uvBoundsMin[faceIdx].x, initUVs[i].x);
                uvBoundsMin[faceIdx].y = Mathf.Min(uvBoundsMin[faceIdx].y, initUVs[i].y);
                uvBoundsMax[faceIdx].x = Mathf.Max(uvBoundsMax[faceIdx].x, initUVs[i].x);
                uvBoundsMax[faceIdx].y = Mathf.Max(uvBoundsMax[faceIdx].y, initUVs[i].y);

                // 座標が近いものは統合していく
                bool unique = true;
                foreach (var uniqueVert in uniqueVerts)
                {
                    if (MathUtils.AreVectorsEqual(dotPoint, uniqueVert))
                    {
                        unique = false;
                        break;
                    }
                }
                if (unique)
                {
                    uniqueVerts.Add(dotPoint);
                    uniqueVertsCenter += dotPoint;
                }
            }
            // 重心とバウンディングボックスを頂点座標から決定
            uniqueVertsCenter /= uniqueVerts.Count;
            Vector2 min = Vector2.zero;
            Vector2 max = Vector2.zero;
            // 多角形の外周を時計回りにソート
            uniqueVerts = MathUtils.SortVerticesCounterClockwise(uniqueVerts);
            foreach (var uniqueVert in uniqueVerts)
            {
                min.x = Mathf.Min(min.x, uniqueVert.x);
                min.y = Mathf.Min(min.y, uniqueVert.y);
                max.x = Mathf.Max(max.x, uniqueVert.x);
                max.y = Mathf.Max(max.y, uniqueVert.y);
            }
            // bounds 決定
            float scaleX = max.x - min.x;
            float scaleY = max.y - min.y;
            float scaleZ = fwdBk.y - fwdBk.x;
            // ヒットした点を中心に円形ランダムプロット
            var sites = new List<Vector2f>();
            var bounds = new Rectf(min.x, min.y, scaleX, scaleY);
            float minScale = Mathf.Min(scaleX, scaleY);
            this.transform.localScale = Vector3.one;
            Vector3 localPos = this.transform.InverseTransformPoint(hit.point);
            for (int i = 0; i < numberOfPoints; i++)
            {
                var r = Random.Range(this.minFragmentSize * 0.5f, this.scaleRadius * minScale);
                var theta = Random.Range(0, 2 * Mathf.PI);
                var x = r * Mathf.Cos(theta);
                var y = r * Mathf.Sin(theta);
                Vector2 point =  new Vector2(localPos.x + x, localPos.y + y);
                if (!MathUtils.IsInsideRect(min, max, point))
                {
                    continue; // 矩形の外側のプロット点はスキップ
                }
                bool near = false;
                foreach (var site in sites)
                {
                    float distance = Vector2.Distance(point, new Vector2(site.x, site.y));
                    if (this.minFragmentSize > distance)
                    {
                        near = true;
                        break;
                    }
                }
                if (!near)
                {
                    sites.Add(new Vector2f(point.x, point.y));
                }
            }
            // プロット点を使ってボロノイ図をバウンディングボックス矩形に描く
            var voronoi = new Voronoi(sites, bounds, 2);
            // ボロノイ図の小領域ごとに破片オブジェクトを作成
            int createdMeshCount = 0;
            var regions = voronoi.Regions();
            voronoi.Dispose();
            if (0 == regions.Count)
            {
                // 分割後の領域が無い場合は作る
                regions.Add(new List<Vector2f>());
            }
            int invalidRegionCount = 0;
            foreach (var region in regions)
            {
                List<Vector2> regionPoints = new List<Vector2>();
                foreach (var r in region)
                {
                    regionPoints.Add(new Vector2(r.x, r.y));
                }
                var vCount = regionPoints.Count;
                // 有効な領域か判定
                bool valid = true;
                if (3 > vCount)
                {
                    valid = false;
                }
                else
                {
                    for (int vIdx = 0; vIdx < vCount; vIdx++)
                    {
                        var coord = regionPoints[vIdx];
                        if (!float.IsFinite(coord.x) || !float.IsFinite(coord.y))
                        {
                            valid = false;
                            break;
                        }
                    }
                }
                if (!valid)
                {
                    invalidRegionCount++;
                    if (regions.Count == invalidRegionCount)
                    {
                        // 破壊不能の場合はスライスのみ
                        regionPoints.Clear();
                        regionPoints.Add(max);
                        regionPoints.Add(new Vector2(min.x, max.y));
                        regionPoints.Add(min);
                        regionPoints.Add(new Vector2(max.x, min.y));
                        vCount = regionPoints.Count;
                    }
                    else
                    {
                        continue;
                    }
                }
                // 領域のバウンディングボックスを求める
                Vector2 fragmentLeftTop = new Vector2(0.5f * scaleX, 0.5f * scaleY);
                Vector2 fragmentRihgtBottom = new Vector2(-0.5f * scaleX, -0.5f * scaleY);
                Vector2 fragmentCenter = Vector2.zero;

                List<Vector2WithIndex> outsideVerts = new List<Vector2WithIndex>();
                for (int vIdx = 0; vIdx < vCount; vIdx++)
                {
                    var vert = regionPoints[vIdx];
                    fragmentCenter += vert;
                    // 破片頂点が多角形の外に出ているかチェック
                    bool outSideFlag = MathUtils.IsOutsidePolygon(uniqueVerts, vert);
                    if (outSideFlag)
                    {
                        outsideVerts.Add(new Vector2WithIndex { i = vIdx, v = vert });
                    }
                    fragmentLeftTop.x = Mathf.Min(fragmentLeftTop.x, vert.x);
                    fragmentLeftTop.y = Mathf.Min(fragmentLeftTop.y, vert.y);
                    fragmentRihgtBottom.x = Mathf.Max(fragmentRihgtBottom.x, vert.x);
                    fragmentRihgtBottom.y = Mathf.Max(fragmentRihgtBottom.y, vert.y);
                }
                float fragmentW = fragmentRihgtBottom.x - fragmentLeftTop.x;
                float fragmentH = fragmentRihgtBottom.y - fragmentLeftTop.y;
                fragmentCenter /= vCount;
                if (outsideVerts.Count == vCount)
                {
                    // 完全に領域外になっている破片は作らない
                    continue;
                }
                if (0 < outsideVerts.Count)
                {
                    // 構成点の一部が多角形の外側にある場合
                    List<Vector2> insideUniqueVerts = new List<Vector2>();
                    foreach (var uniqueVert in uniqueVerts)
                    {
                        // 領域内に多角形の頂点を含むかどうか
                        if (!MathUtils.IsOutsidePolygon(regionPoints, uniqueVert))
                        {
                            insideUniqueVerts.Add(uniqueVert);
                        }
                    }
                    foreach (var outsideVert in outsideVerts)
                    {
                        // 領域外に出ている線分を頂点座標を領域境界上まで縮める
                        var v = outsideVert.v;
                        var intersection = MathUtils.GetIntersectionOnPolygonEdge(uniqueVerts, uniqueVertsCenter, new Vector2(v.x, v.y));
                        if (Vector2.zero != intersection)
                        {
                            v.x = intersection.x;
                            v.y = intersection.y;
                        }
                        regionPoints[outsideVert.i] = v;
                    }
                    foreach (var insideUniqueVert in insideUniqueVerts)
                    {
                        // 多角形の頂点が領域内にあった場合は頂点を領域の頂点に加える
                        regionPoints.Add(insideUniqueVert);
                        regionPoints = MathUtils.SortVerticesCounterClockwise(regionPoints);
                    }
                }

                bool isConvex = MathUtils.IsConvex(regionPoints);
                if (!isConvex)
                {
                    continue; // 凹形状の破片は作らない
                }

                // 三角形の頂点インデックスリストを先に決定
                vCount = regionPoints.Count;
                int extraIdx = vCount % 2;
                int trisCount = 12 * (vCount - 1);
                var tris = new int[trisCount];
                var t = 0;
                for (int vIdx = 1; vIdx < vCount - 1; vIdx++)
                {
                    // back face
                    tris[t++] = 0 * vCount + 0;
                    tris[t++] = 0 * vCount + vIdx;
                    tris[t++] = 0 * vCount + vIdx + 1;
                    // front face
                    tris[t++] = 1 * vCount + vIdx + 1;
                    tris[t++] = 1 * vCount + vIdx;
                    tris[t++] = 1 * vCount + 0;
                }
                for (int vIdx = 0; vIdx < vCount; vIdx++)
                {
                    bool loop = vIdx == (vCount - 1);
                    if (vIdx == (vCount - 1))
                    {
                        if (1 == extraIdx)
                        {
                            int skipIdx = 2 * (vIdx % 2);
                            // edgeFace1
                            tris[t++] = (2 + skipIdx) * vCount + vIdx;
                            tris[t++] = (3 + skipIdx) * vCount + vIdx;
                            tris[t++] = 6 * vCount + 0;
                            // edgeFace2
                            tris[t++] = 6 * vCount + 0;
                            tris[t++] = 6 * vCount + 1;
                            tris[t++] = (2 + skipIdx) * vCount + vIdx;
                        }
                        else
                        {
                            int skipIdx = 2 * (vIdx % 2);
                            // edgeFace1
                            tris[t++] = (2 + skipIdx) * vCount + vIdx;
                            tris[t++] = (3 + skipIdx) * vCount + vIdx;
                            tris[t++] = (3 + skipIdx) * vCount + 0;
                            // edgeFace2
                            tris[t++] = (3 + skipIdx) * vCount + 0;
                            tris[t++] = (2 + skipIdx) * vCount + 0;
                            tris[t++] = (2 + skipIdx) * vCount + vIdx;
                        }
                    }
                    else
                    {
                        int skipIdx = 2 * (vIdx % 2);
                        // edgeFace1
                        tris[t++] = (2 + skipIdx) * vCount + vIdx;
                        tris[t++] = (3 + skipIdx) * vCount + vIdx;
                        tris[t++] = (3 + skipIdx) * vCount + vIdx + 1;
                        // edgeFace2
                        tris[t++] = (3 + skipIdx) * vCount + vIdx + 1;
                        tris[t++] = (2 + skipIdx) * vCount + vIdx + 1;
                        tris[t++] = (2 + skipIdx) * vCount + vIdx;
                    }                    
                }

                // 巻き割りしたような破片が作られる場合は、柱をランダムな面でスライスします
                int meshCount = 1;
                if (minScale <= scaleZ * 3)
                {
                    var wh = Mathf.Max(fragmentW, fragmentH, scaleZ / 8f);
                    meshCount = Mathf.CeilToInt(scaleZ / wh);
                    meshCount = Random.Range(meshCount, meshCount + 2);
                }
                // 乱数でゆらぎを与えた切断面の位置や法線をあらかじめ作っておく
                float sliceDepth = scaleZ / meshCount;
                float[] sliceDepthArray = new float[meshCount + 1];
                sliceDepthArray[0] = 0;
                sliceDepthArray[meshCount] = scaleZ;
                for (int i = 1; i < meshCount; i++)
                {
                    sliceDepthArray[i] = (sliceDepth * i) + Random.Range(-sliceDepth, sliceDepth) * 0.1f;
                }
                Vector3[] planeNornals = new Vector3[meshCount + 1];
                planeNornals[0] = planeNornals[meshCount] = -Vector3.forward;
                for (int i = 1; i < meshCount; i++)
                {
                    planeNornals[i] = Quaternion.Euler(Random.Range(-35f, 35f), Random.Range(-35f, 35f), 0) * -Vector3.forward;
                }
                Vector3 planePoint = Vector3.zero;
                for (int vIdx = 0; vIdx < vCount; vIdx++)
                {
                    var coord = regionPoints[vIdx];
                    planePoint += new Vector3(coord.x, coord.y, 0);
                }
                planePoint /= vCount;
                // スライスされるオブジェクトの数ループ
                for (int zIdx = 0; zIdx < meshCount; zIdx++)
                {
                    var mesh = new Mesh();
                    var verts = new Vector3[6 * vCount + 2 * extraIdx];
                    var uvs = new Vector2[verts.Length];
                    
                    Vector3 planePoint0 = planePoint;
                    planePoint0.z = -0.5f * scaleZ + sliceDepthArray[zIdx];
                    Vector3 planeNornal0 = planeNornals[zIdx];
                    Vector3 planePoint1 = planePoint;
                    planePoint1.z = -0.5f * scaleZ + sliceDepthArray[zIdx + 1];
                    Vector3 planeNornal1 = planeNornals[zIdx + 1];
                    float centerZ = 0;
                    bool setLastUV = true;
                    for (int vIdx = 0; vIdx < vCount; vIdx++)
                    {
                        var coord = regionPoints[vIdx];
                        int skipIdx = 2 * (vIdx % 2);
                        Vector3 linePoint = new Vector3(coord.x, coord.y, -0.5f * scaleZ);
                        Vector3 intersection0 = MathUtils.PlaneIntersection(planePoint0, planeNornal0, linePoint, Vector3.forward, scaleZ);
                        float sliceDepth0 = intersection0.z;
                        Vector3 intersection1 = MathUtils.PlaneIntersection(planePoint1, planeNornal1, linePoint, Vector3.forward, scaleZ);
                        float sliceDepth1 = intersection1.z;
                        // 頂点座標の決定
                        verts[1 * vCount + vIdx] = verts[3 * vCount + vIdx] = verts[5 * vCount + vIdx] = new Vector3(coord.x, coord.y, sliceDepth0);
                        verts[0 * vCount + vIdx] = verts[2 * vCount + vIdx] = verts[4 * vCount + vIdx] = new Vector3(coord.x, coord.y, sliceDepth1);
                        centerZ += (sliceDepth0 + sliceDepth1) * 0.5f;
                        // UV座標の決定
                        float lerpX = (coord.x + 0.5f * scaleX) / scaleX;
                        float lerpY = (coord.y + 0.5f * scaleY) / scaleY;
                        float lerpZ0 = (sliceDepth0 + 0.5f * scaleZ) / scaleZ;
                        float lerpZ1 = (sliceDepth1 + 0.5f * scaleZ) / scaleZ;
                        // 表面と裏面について UV 座標を打つ
                        for (int faceIdx = 0; faceIdx < 2; faceIdx++)
                        {
                            float uvX = Mathf.Lerp(uvBoundsMin[faceIdx].x, uvBoundsMax[faceIdx].x, 1 - lerpX);
                            float uvY = Mathf.Lerp(uvBoundsMin[faceIdx].y, uvBoundsMax[faceIdx].y, (1 == faceIdx) ? 1 - lerpY : lerpY);
                            uvs[faceIdx * vCount + vIdx] = new Vector2(uvX, uvY) + new Vector2(0, (1 - uvBoundsMax[faceIdx].y) - uvBoundsMin[faceIdx].y);
                        }
                        // 側面の UV 座標は X 方向の面なのか Y 方向の面なのかを判定してからサンプルする軸方向の UV 値を決定する
                        bool isLoop = vCount == vIdx + 1;
                        int nextVIdx = isLoop ? 0 : vIdx + 1;
                        var nextCoord = regionPoints[nextVIdx];
                        Vector3 nextlinePoint = new Vector3(nextCoord.x, nextCoord.y, -0.5f * scaleZ);
                        Vector3 nextIntersection0 = MathUtils.PlaneIntersection(planePoint0, planeNornal0, nextlinePoint, Vector3.forward, scaleZ);
                        float nextSliceDepth0 = nextIntersection0.z;
                        Vector3 nextIntersection1 = MathUtils.PlaneIntersection(planePoint1, planeNornal1, nextlinePoint, Vector3.forward, scaleZ);
                        float nextSliceDepth1 = nextIntersection1.z;
                        float nextLerpX = (nextCoord.x + 0.5f * scaleX) / scaleX;
                        float nextLerpY = (nextCoord.y + 0.5f * scaleY) / scaleY;
                        float nextLerpZ0 = (nextSliceDepth0 + 0.5f * scaleZ) / scaleZ;
                        float nextLerpZ1 = (nextSliceDepth1 + 0.5f * scaleZ) / scaleZ;
                        var sideFace = this.GetSideFaceType(coord, nextCoord);
                        switch (sideFace)
                        {
                            case FaceType.Left:
                                {
                                    int srcFaceIdx = (int)FaceType.Left;
                                    float uvYCurr = Mathf.Lerp(uvBoundsMin[srcFaceIdx].y, uvBoundsMax[srcFaceIdx].y, lerpY);
                                    float uvYNext = Mathf.Lerp(uvBoundsMin[srcFaceIdx].y, uvBoundsMax[srcFaceIdx].y, nextLerpY);
                                    uvs[(2 + skipIdx) * vCount + vIdx] = new Vector2(1 - lerpZ1, uvYCurr);
                                    uvs[(3 + skipIdx) * vCount + vIdx] = new Vector2(1 - lerpZ0, uvYCurr);
                                    uvs[(2 + skipIdx) * vCount + nextVIdx] = new Vector2(1 - nextLerpZ1, uvYNext);
                                    uvs[(3 + skipIdx) * vCount + nextVIdx] = new Vector2(1 - nextLerpZ0, uvYNext);
                                }
                                break;
                            case FaceType.Right:
                                {
                                    int srcFaceIdx = (int)FaceType.Right;
                                    float uvYCurr = Mathf.Lerp(uvBoundsMin[srcFaceIdx].y, uvBoundsMax[srcFaceIdx].y, lerpY);
                                    float uvYNext = Mathf.Lerp(uvBoundsMin[srcFaceIdx].y, uvBoundsMax[srcFaceIdx].y, nextLerpY);
                                    uvs[(3 + skipIdx) * vCount + vIdx] = new Vector2(lerpZ0, uvYCurr);
                                    uvs[(2 + skipIdx) * vCount + vIdx] = new Vector2(lerpZ1, uvYCurr);
                                    if (isLoop && 1 == extraIdx) //  && FaceType.Top == initFaceType
                                    {
                                        uvs[6 * vCount + 0] = new Vector2(nextLerpZ0, uvYNext);
                                        uvs[6 * vCount + 1] = new Vector2(nextLerpZ1, uvYNext);
                                        setLastUV = false;
                                    }
                                    else
                                    {
                                        uvs[(3 + skipIdx) * vCount + nextVIdx] = new Vector2(nextLerpZ0, uvYNext);
                                        uvs[(2 + skipIdx) * vCount + nextVIdx] = new Vector2(nextLerpZ1, uvYNext);
                                    }
                                }
                                break;
                            case FaceType.Bottom:
                                {
                                    int srcFaceIdx = (int)FaceType.Bottom;
                                    float uvXCurr = Mathf.Lerp(uvBoundsMin[srcFaceIdx].x, uvBoundsMax[srcFaceIdx].x, 1 - lerpX);
                                    float uvXNext = Mathf.Lerp(uvBoundsMin[srcFaceIdx].x, uvBoundsMax[srcFaceIdx].x, 1 - nextLerpX);
                                    uvs[(3 + skipIdx) * vCount + vIdx] = new Vector2(uvXCurr, lerpZ0);
                                    uvs[(2 + skipIdx) * vCount + vIdx] = new Vector2(uvXCurr, lerpZ1);

                                    if (isLoop && 1 == extraIdx) //  && FaceType.Top == initFaceType
                                    {
                                        uvs[6 * vCount + 0] = new Vector2(uvXNext, nextLerpZ0);
                                        uvs[6 * vCount + 1] = new Vector2(uvXNext, nextLerpZ1);
                                        setLastUV = false;
                                    }
                                    else
                                    {
                                        
                                        uvs[(3 + skipIdx) * vCount + nextVIdx] = new Vector2(uvXNext, nextLerpZ0);
                                        uvs[(2 + skipIdx) * vCount + nextVIdx] = new Vector2(uvXNext, nextLerpZ1);
                                    }
                                }
                                break;
                            case FaceType.Top:
                                {
                                    int srcFaceIdx = (int)FaceType.Top;
                                    float uvXCurr = Mathf.Lerp(uvBoundsMin[srcFaceIdx].x, uvBoundsMax[srcFaceIdx].x, 1 - lerpX);
                                    float uvXNext = Mathf.Lerp(uvBoundsMin[srcFaceIdx].x, uvBoundsMax[srcFaceIdx].x, 1 - nextLerpX);
                                    uvs[(2 + skipIdx) * vCount + vIdx] = new Vector2(uvXCurr, 1 - lerpZ1);
                                    uvs[(3 + skipIdx) * vCount + vIdx] = new Vector2(uvXCurr, 1 - lerpZ0);
                                    uvs[(2 + skipIdx) * vCount + nextVIdx] = new Vector2(uvXNext, 1 - nextLerpZ1);
                                    uvs[(3 + skipIdx) * vCount + nextVIdx] = new Vector2(uvXNext, 1 - nextLerpZ0);
                                }
                                break;
                        }
                    }
                    centerZ /= vCount;
                    if (1 == extraIdx)
                    {
                        // 周回する場合はエクストラ頂点座標を追加指定
                        verts[6 * vCount + 0] = verts[3 * vCount];
                        verts[6 * vCount + 1] = verts[2 * vCount];
                        if (setLastUV)
                        {
                            uvs[6 * vCount + 0] = uvs[3 * vCount];
                            uvs[6 * vCount + 1] = uvs[2 * vCount];
                        }
                    }
                    Vector3 v3Center = new Vector3(fragmentCenter.x, fragmentCenter.y, centerZ);
                    for (int i = 0; i < verts.Length; i++)
                    {
                        verts[i] -= v3Center;
                    }
                    // メッシュを頂点座標と UV 値から作成して、オブジェクト化
                    mesh.vertices = verts;
                    mesh.triangles = tris;
                    mesh.uv = uvs;
                    mesh.RecalculateBounds();
                    mesh.RecalculateNormals();
                    mesh.RecalculateTangents();

                    var fragment = Instantiate(this.fragmentPrefab);
                    fragment.name = this.name + "_fragment" + zIdx + ((1 == extraIdx) ? "extra" : "");
                    fragment.transform.SetParent(this.transform, false);
                    fragment.transform.localPosition = v3Center;
                    fragment.transform.localRotation = Quaternion.identity;
                    fragment.transform.localScale = Vector3.one;
                    fragment.transform.SetParent(null);
                    fragment.sharedMesh = mesh;

                    if (this.TryGetComponent(out MeshRenderer myMeshRenderer))
                    {
                        if (fragment.TryGetComponent(out MeshRenderer meshRenderer))
                        {
                            meshRenderer.material = new Material(myMeshRenderer.material);
                        }
                    }
                    if (fragment.TryGetComponent(out MeshCollider meshCollider))
                    {
                        meshCollider.sharedMesh = mesh;
                    }
                    fragment.gameObject.AddComponent<MeshCleaner>();
                    
                    float reFragmentSize = scaleZ * 1.5f;
                    if (reFragmentSize < fragmentW && reFragmentSize < fragmentH)
                    {
                        var cubeFragment = fragment.gameObject.AddComponent<VoronoiFragmenter>();
                        cubeFragment.fragmentPrefab = this.fragmentPrefab;
                        cubeFragment.numberOfPoints = this.numberOfPoints;
                        cubeFragment.scaleRadius = this.scaleRadius;
                        cubeFragment.minFragmentSize = this.minFragmentSize;
                        cubeFragment.fadeCurve = this.fadeCurve;
                        cubeFragment.fadeDuration = this.fadeDuration;
                        cubeFragment.remainingTime = this.remainingTime;
                    }
                    else
                    {
                        var fadeOuter = fragment.gameObject.AddComponent<FadeOuter>();
                        fadeOuter.FadeOut(Random.Range(this.remainingTime, this.remainingTime * 1.3f), this.fadeDuration, this.fadeCurve);
                    }
                    createdMeshCount++;
                }
            }

            this.transform.localScale = scale;
            if (0 < createdMeshCount)
            {
                // 自オブジェクトのコライダーとメッシュ非表示
                if (this.TryGetComponent(out Collider collider))
                {
                    Destroy(collider);
                }
                if (this.TryGetComponent(out Renderer renderer))
                {
                    renderer.enabled = false;
                }
                List<Transform> childs = new List<Transform>();
                foreach (Transform childTransform in this.transform)
                {
                    childs.Add(childTransform);
                }
                foreach (Transform childTransform in childs)
                {
                    // 子オブジェクトを開放
                    if (!childTransform.TryGetComponent(out Rigidbody rigidbody))
                    {
                        childTransform.gameObject.AddComponent<Rigidbody>();
                    }
                    childTransform.SetParent(null, true);
                }
            }
        }

        /// <summary>
        /// 法線ベクトルから面インデックスを決定する
        /// </summary>
        /// <param name="normal">面の法線</param>
        /// <returns>面のインデックス 0 ~ 5</returns>
        FaceType GetNormalFaceIndex(Vector3 normal)
        {
            if (-1 == normal.z)
            {
                return FaceType.Front;
            }
            else if(1 == normal.z)
            {
                return FaceType.Back;
            }
            else if (-1 == normal.x)
            {
                return FaceType.Left;
            }
            else if (1 == normal.x)
            {
                return FaceType.Right;
            }
            else if (-1 == normal.y)
            {
                return FaceType.Bottom;
            }
            else if (1 == normal.y)
            {
                return FaceType.Top;
            }
            return FaceType.Front;
        }

        /// <summary>
        /// 反時計回りの多角形頂点2点からわかる側面の方向
        /// </summary>
        /// <param name="coord">開始点</param>
        /// <param name="nextCoord">終了点</param>
        /// <returns>側面タイプ</returns>
        FaceType GetSideFaceType(Vector2 coord, Vector2 nextCoord)
        {
            var diff = nextCoord - coord;
            if(0 == diff.x)
            {
                if (diff.y > 0)
                {
                    return FaceType.Right;
                }
                else
                {
                    return FaceType.Left;
                }
            }
            else if (0 == diff.y)
            {
                if (diff.x > 0)
                {
                    return FaceType.Bottom;
                }
                else
                {
                    return FaceType.Top;
                }
            }
            else
            {
                // 上面下面のテクスチャを使う
                if (diff.x > 0)
                {
                    return FaceType.Bottom;
                }
                else
                {
                    return FaceType.Top;
                }
            }
        }

        struct Vector2WithIndex
        {
            public int i;
            public Vector2 v;
        }

        enum FaceType
        {
            Front,
            Back,
            Left,
            Right,
            Bottom,
            Top,
            Max
        }
    }
}
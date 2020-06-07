using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParkourObstacle
{
    private Wall _floor;

    private Wall[] _walls;
    private BoxCollider _collider;

    public BoxCollider Collider
    {
        get => _collider;
    }

    public ParkourObstacle(Wall floor, Wall[] walls, BoxCollider collider)
    {
        _floor = floor;
        _walls = walls;
        _collider = collider;
    }

    public bool TryGetWall(Vector3 point, ref Wall wall)
    {
        for (int i = 0; i < _walls.Length; i++)
        {
            Plane wallPlane = new Plane(_walls[i].GetPoint(0), _walls[i].GetPoint(1), _walls[i].GetPoint(2));
            if (wallPlane.ClosestPointOnPlane(point) == point)
            {
                wall = _walls[i];
                return true;
            }
        }
        return false;
    }

    public struct Wall
    {
        public Plane Plane
        {
            get => new Plane(_point0, _point1, _point2);
        }

        private Vector3 _point0;
        private Vector3 _point1;
        private Vector3 _point2;
        private Vector3 _point3;
        private const int _pointsCount = 4;

        public Wall(Vector3 point0, Vector3 point1, Vector3 point2, Vector3 point3)
        {
            _point0 = point0;
            _point1 = point1;
            _point2 = point2;
            _point3 = point3;
        }

        public Vector3 GetHighestPoint()
        {
            Vector3 maxPoint = Vector3.negativeInfinity;
            for (int i = 0; i < _pointsCount; i++)
            {
                var currentPoint = GetPoint(i);
                if (currentPoint.y > maxPoint.y)
                {
                    maxPoint = currentPoint;
                }
            }
            return maxPoint;
        }

        public Vector3 GetPoint(int index)
        {
            switch (index)
            {
                case 0: return _point0;
                case 1: return _point1;
                case 2: return _point2;
                case 3: return _point3;
                default: throw new System.Exception("Wall::GetPoint index out of range");
            }
        }

        public override bool Equals(object obj)
        {
            return obj is Wall wall &&
                _point0.Equals(wall._point0) &&
                _point1.Equals(wall._point1) &&
                _point2.Equals(wall._point2) &&
                _point3.Equals(wall._point3);
        }
    }
}

public class ParkourEntryPoint
{
    public Vector3 Point { get => _point; set => _point = value; }
    public ParkourObstacle.Wall Wall { get => _wall; }
    public ParkourObstacle ParkourObstacle { get => _parkourObstacle; }
    public bool IsClimbing { get => _isClimbing; }

    private Vector3 _point;
    private ParkourObstacle.Wall _wall;
    private ParkourObstacle _parkourObstacle;
    private bool _isClimbing;

    public ParkourEntryPoint(Vector3 point, ParkourObstacle.Wall wall, ParkourObstacle parkourObstacle, bool isClimbing)
    {
        _point = point;
        _wall = wall;
        _parkourObstacle = parkourObstacle;
        _isClimbing = isClimbing;
    }

    public ParkourEntryPoint(ParkourEntryPoint copyRef)
    {
        _point = copyRef._point;
        _wall = copyRef._wall;
        _parkourObstacle = copyRef._parkourObstacle;
        _isClimbing = copyRef._isClimbing;
    }
}

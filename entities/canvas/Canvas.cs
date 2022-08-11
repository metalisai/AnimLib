using System;

namespace AnimLib {
    public class Canvas {
        Vector3 _center, _normal;
        float _width, _height;

        // identity - x is width, y is height, z is flat/depth
        public Canvas(Vector3 center, Quaternion rotation, Vector2 size) {
        }

        // only possible when canvas is parallel to camera near plane
        public M3x3 GetAffineTransform(PerspectiveCamera cam, Vector2 screenPos, Vector2 screenSize) {
            throw new NotImplementedException();
        }
    }
}

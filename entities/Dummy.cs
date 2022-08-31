
namespace AnimLib {
    public class DummyState : EntityState3D {
        public DummyState() : base() {
        }

        public DummyState(DummyState d) : base(d) {
        }

        public override object Clone() {
            return new DummyState(this);
        }
    }

    public class Dummy : VisualEntity3D {
        public Dummy() : base(new DummyState()) {}

        public Dummy(Dummy dummy) : base(dummy) {}

        public override object Clone() {
            return new Dummy(this);
        }
    }
}

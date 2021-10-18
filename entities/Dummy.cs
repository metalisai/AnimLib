
namespace AnimLib {
    public class Dummy : VisualEntity {
        public Dummy() {}

        public Dummy(Dummy dummy) : base(dummy) {}

        public override object Clone() {
            return new Dummy(this);
        }
    }
}

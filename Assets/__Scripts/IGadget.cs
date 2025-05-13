public interface IGadget {
    bool GadgetUse( Dray tDray, System.Func<IGadget, bool> tDoneCallback );
    bool GadgetCancel();
    string name { get; }
}

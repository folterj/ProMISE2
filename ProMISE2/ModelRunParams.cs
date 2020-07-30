
namespace ProMISE2
{
    class ModelRunParams
    {
        public InParamsExt inParams;
        public ViewParams viewParams;
        public OptionParams optionParams;
        public ViewType viewType;
        public bool updateModelReq;
        public bool updateOutReq;

        public ModelRunParams(InParamsExt inParams, ViewParams viewParams, OptionParams optionParams, ViewType viewType, bool updateModelReq, bool updateOutReq)
        {
            this.inParams = inParams;
            this.viewParams = viewParams;
            this.optionParams = optionParams;
            this.viewType = viewType;
            this.updateModelReq = updateModelReq;
            this.updateOutReq = updateOutReq;
        }

    }
}

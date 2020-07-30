using System.Collections;
using System.Threading;

namespace ProMISE2
{
    public interface PreviewObserver
    {
        void previewUpdate(OutParams outparams);
    }

    public interface ModelObserver
    {
        void modelUpdate(OutParams outparams);
        void clearProgress();
        void updateProgress(float progress);
    }

    public abstract class ProModelInterface
    {
        public PreviewModel preview;
        public Model model;

        public ArrayList previewobservers;
        public ArrayList modelobservers;

        public void registerPreviewObserver(ModelObserver observer)
        {
            previewobservers.Add(observer);
        }

        public void unregisterPreviewObserver(ModelObserver observer)
        {
            previewobservers.Remove(observer);
        }

        public void registerModelObserver(ModelObserver observer)
        {
            modelobservers.Add(observer);
        }

        public void unregisterModelObserver(ModelObserver observer)
        {
            modelobservers.Remove(observer);
        }

		public abstract void writeData(string fileName, ViewParams viewParams, int timei);
		
		public abstract void updatePreview(InParamsExt inParams, ViewParams viewParams, OptionParams optionParams);
        public abstract void updateModel(InParamsExt inParams, ViewParams viewParams, OptionParams optionParams, ViewType viewType, bool updateModelReq, bool updateOutReq);
		public abstract bool abortModel();
		public abstract bool isRunning();

        public abstract void clearProgress();
        public abstract void updateProgress(float progress);
    }

    public class ProModel : ProModelInterface
    {
		Thread modelThread;

		public ProModel()
        {
            previewobservers = new ArrayList();
            modelobservers = new ArrayList();
        }

        public override void updatePreview(InParamsExt inParams, ViewParams viewParams, OptionParams optionParams)
        {
            if (preview == null)
            {
                preview = new PreviewModel(this, inParams, optionParams);
            }
            preview.update(inParams, null);
            preview.updateOut(viewParams);
            preview.stats.start();
            updatePreviewObservers();
            preview.stats.storeDrawviewTime();
        }

        public override void updateModel(InParamsExt inParams, ViewParams viewParams, OptionParams optionParams, ViewType viewType, bool updateModelReq, bool updateOutReq)
        {
            modelThread = new Thread(new ParameterizedThreadStart(updateModelThreadFunction));
            modelThread.Start(new ModelRunParams(inParams, viewParams, optionParams, viewType, updateModelReq, updateOutReq));
        }

        public void updateModelThreadFunction(object modelRunParams0)
        {
            ModelRunParams modelRunParams = (ModelRunParams)modelRunParams0;

            // first check if model needs creating
            bool create = true;
            if (model != null)
            {
                switch (modelRunParams.inParams.model)
                {
                    case ModelType.CCD:
                        create = (model.GetType() != typeof(CCDModel));
                        break;
                    case ModelType.Probabilistic:
                        create = (model.GetType() != typeof(ProbModel));
                        break;
                    case ModelType.Transport:
                        create = (model.GetType() != typeof(TransModel));
                        break;
                }
            }

            if (create)
            {
                switch (modelRunParams.inParams.model)
                {
                    case ModelType.CCD:
                        model = new CCDModel(this, modelRunParams.inParams, preview.outParams, modelRunParams.optionParams);
                        break;
                    case ModelType.Probabilistic:
                        model = new ProbModel(this, modelRunParams.inParams, preview.outParams, modelRunParams.optionParams);
                        break;
                    case ModelType.Transport:
                        model = new TransModel(this, modelRunParams.inParams, preview.outParams, modelRunParams.optionParams);
                        break;
                    default:
                        model = new PreviewModel(this, modelRunParams.inParams, modelRunParams.optionParams);
                        break;
                }
                // if  model just created; model needs to be updated too
                modelRunParams.updateModelReq = true;
            }

            if (modelRunParams.updateModelReq || modelRunParams.updateOutReq)
            {
                // clear previous output
                if (modelRunParams.viewType == ViewType.Out)
                {
                    model.clearOut();
                }
                else if (modelRunParams.viewType == ViewType.Time)
                {
                    model.clearTimeOut();
                }
                updateModelObservers();
            }

            if (modelRunParams.updateModelReq)
            {
                preview.update(modelRunParams.inParams, null);
                preview.updateOut(modelRunParams.viewParams);
                model.update(modelRunParams.inParams, preview.previewParams);
                // if model updated, out must be updated too
                modelRunParams.updateOutReq = true;
            }

            if (modelRunParams.updateOutReq)
            {
                if (modelRunParams.viewType == ViewType.Out)
                {
                    model.updateOut(modelRunParams.viewParams);
                }
                else if (modelRunParams.viewType == ViewType.Time)
                {
                    model.updateTimeOut(modelRunParams.viewParams);
                }
                model.stats.start();
                updateModelObservers();
                model.stats.storeDrawviewTime();
            }

			Util.gcCollect();
        }

		public override bool isRunning()
		{
			if (model != null)
			{
				return model.running;
			}
			return false;
		}

		public override bool abortModel()
		{
			bool aborted = false;

			if (model != null)
			{
				aborted = model.running;
				model.running = false;
			}
			return aborted;
		}

		public override void writeData(string fileName, ViewParams viewParams, int timei)
		{
			if (model != null)
			{
				model.writeData(fileName, viewParams, timei);
			}
		}

        public override void clearProgress()
        {
            for (int i = 0; i < modelobservers.Count; i++)
            {
                ((ModelObserver)modelobservers[i]).clearProgress();
            }
        }

        public override void updateProgress(float progress)
        {
            for (int i = 0; i < modelobservers.Count; i++)
            {
                ((ModelObserver)modelobservers[i]).updateProgress(progress);
            }
        }

		public void updatePreviewObservers()
        {
            for (int i = 0; i < previewobservers.Count; i++)
            {
                ((PreviewObserver)previewobservers[i]).previewUpdate(preview.outParams);
            }
        }

        public void updateModelObservers()
        {
            for (int i = 0; i < modelobservers.Count; i++)
            {
                ((ModelObserver)modelobservers[i]).modelUpdate(model.outParams);
            }
        }

    }
}

using System.Collections.Generic;

namespace ProMISE2
{
    public abstract class CCDCells : List<Cell>
    {
        public InParamsExt inParams;

        public CCDCells(InParamsExt inParams)
        {
            this.inParams = inParams;
        }

        public CCDCells(CCDCells ccdCells)
        {
            int len = ccdCells.Count;
            this.Clear();
            for (int i = 0; i < len; i++)
            {
                this.Add(new Cell(ccdCells[i].m, ccdCells[i].zone));
            }
            inParams = ccdCells.inParams;
        }

        public Cell justOut()
        {
            int pos = this.Count - inParams.column2 - 1;
            return this[pos];
        }

        public float getNorm(int pos)
        {
            return this[(int)normToModel(pos, false)].m;
        }

        public void setNorm(int pos, float m)
        {
            this[(int)normToModel(pos, false)].m = m;
        }

        public float norm2ToModel(float pos, int ncol, bool inverse)
        {
            return normToModel(norm2ToNorm(pos, ncol), inverse);
        }

        public abstract float normToModel(float pos, bool inverse);

        public abstract float norm2ToNorm(float pos0, int ncol);

        public abstract float normToNorm2(float pos0, int ncol, bool inverse);

        public float getNorm2(float pos0, int ncol, bool inverse, float[] filterWeight)
        {
            float m = 0;
            float pos = norm2ToModel(pos0, ncol, inverse);
            float pos2;
            float posf;

            if (pos >= 0 && pos < this.Count)
            {
                if (inParams.autoFilter && filterWeight.Length > 1)
                {
                    posf = pos - (int)pos;
                    for (int i = 0; i < filterWeight.Length - 1; i++)
                    {
                        pos2 = pos + i - filterWeight.Length / 2;
                        if (pos2 >= 0 && pos2 < this.Count)
                        {
                            m += this[(int)pos2].m * (filterWeight[i] * posf + filterWeight[i + 1] * (1 - posf));
                        }
                    }
                }
                else
                {
                    m = this[(int)pos].m;
                }
            }
            return normStepMass(m);
        }

        public int getNorm2zone(float pos0, int ncol, bool inverse)
        {
            int zone = 0;
            float pos = norm2ToModel(pos0, ncol, inverse);

            if (pos >= 0 && pos < this.Count)
            {
                zone = this[(int)pos].zone;
            }
            return zone;
        }

        public abstract float normStepMass(float m);

        public abstract float getStepVol(float pos0, int ncol, bool inverse);
    }


    public class UCCDCells : CCDCells
    {
        public UCCDCells(InParamsExt inparams)
            : base(inparams)
        {
        }

        public UCCDCells(UCCDCells ccdCells)
            : base(ccdCells)
        {
        }

        public override float normToModel(float pos, bool inverse)
        {
            // pos: norm -> model
            bool timemode = inverse || (inParams.runMode == RunModeType.CoCurrent);
            if (timemode)
            {
                return (this.Count - inParams.column2) + pos;
            }
            return (this.Count - 1) - pos;
        }

        public override float norm2ToNorm(float pos0, int ncol)
        {
            // pos: norm2 -> norm
            float pos = pos0 - ncol;
            if (inParams.fnormu > 0)
            {
                if (pos > inParams.column2)
                {
                    pos -= inParams.column2;
                    pos *= inParams.fnormu;
                    pos += inParams.column2;
                }
                else if (pos < 0)
                {
                    pos *= inParams.fnormu;
                }
            }
            return pos;
        }

        public override float normToNorm2(float pos0, int ncol, bool inverse)
        {
            // pos: norm -> norm2
            float pos = pos0;
            if (inParams.fnormu > 0)
            {
                if (pos > inParams.column2)
                {
                    pos -= inParams.column2;
                    pos /= inParams.fnormu;
                    pos += inParams.column2;
                }
                else if (pos < 0)
                {
                    pos /= inParams.fnormu;
                }
            }
            if (inverse && !(inParams.runMode == RunModeType.CoCurrent))
            {
                return ncol + inParams.column2 - pos;
            }
            return ncol + pos;
        }

        public override float normStepMass(float m)
        {
            // normalise mass
            if (inParams.fnormu > 0)
            {
                return m * inParams.fnormu;
            }
            return m;
        }

        public override float getStepVol(float pos0, int ncol, bool inverse)
        {
            float pos = norm2ToNorm(pos0, ncol);
            float modelpos = normToModel(pos, inverse);

            if (modelpos >= 0 && modelpos < this.Count)
            {
                if (pos > inParams.column2 || pos < 0)
                {
                    return (inParams.vu * inParams.fnormu) / inParams.column;
                }
                else
                {
                    return inParams.vu / inParams.column;
                }
            }
            return 0;
        }
    }


    public class LCCDCells : CCDCells
    {
        public LCCDCells(InParamsExt inparams)
            : base(inparams)
        {
        }

        public LCCDCells(CCDCells ccdCells)
            : base(ccdCells)
        {
        }

        public override float normToModel(float pos, bool inverse)
        {
            // pos: norm -> model
            bool timemode = !inverse;
            if (timemode)
            {
                return (this.Count - inParams.column2) + pos;
            }
            return (this.Count - 1) - pos;
        }

        public override float norm2ToNorm(float pos0, int ncol)
        {
            // pos: norm2 -> norm
            float pos = pos0 - ncol;
            if (inParams.fnorml > 0)
            {
                if (pos > inParams.column2)
                {
                    pos -= inParams.column2;
                    pos *= inParams.fnorml;
                    pos += inParams.column2;
                }
                else if (pos < 0)
                {
                    pos *= inParams.fnorml;
                }
            }
            return pos;
        }

        public override float normToNorm2(float pos0, int ncol, bool inverse)
        {
            // pos: norm -> norm2
            float pos = pos0;
            if (inParams.fnorml > 0)
            {
                if (pos > inParams.column2)
                {
                    pos -= inParams.column2;
                    pos /= inParams.fnorml;
                    pos += inParams.column2;
                }
                else if (pos < 0)
                {
                    pos /= inParams.fnorml;
                }
            }
            if (inverse)
            {
                return ncol + inParams.column2 - pos;
            }
            return ncol + pos;
        }

        public override float normStepMass(float m)
        {
            // normalise mass
            if (inParams.fnorml > 0)
            {
                return m * inParams.fnorml;
            }
            return m;
        }

        public override float getStepVol(float pos0, int ncol, bool inverse)
        {
            float pos = norm2ToNorm(pos0, ncol);
            float modelpos = normToModel(pos, inverse);

            if (modelpos >= 0 && modelpos < this.Count)
            {
                if (pos > inParams.column2 || pos < 0)
                {
                    return (inParams.vl * inParams.fnorml) / inParams.column;
                }
                else
                {
                    return inParams.vl / inParams.column;
                }
            }
            return 0;
        }

    }
}

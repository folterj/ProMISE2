using System.Collections.Generic;

namespace ProMISE2
{
    public class TransCon : List<float> // replace with vector<Cell^> for Int mode zones; expect significant impact on performance
    {
        private InParamsExt inParams;

        public TransCon()
        {
        }

        public TransCon(InParamsExt inparams)
        {
            this.inParams = inparams;
        }

        public TransCon(TransCon con)
        {
            Clear();
            foreach (float x in con)
            {
                Add(x);
            }
            inParams = con.inParams;
        }

        public float get(int i)
        {
            if (i >= 0 && i < Count)
            {
                return this[i];
            }
            return 0;
        }

        public float getNorm(int i, int offset, bool inverse)
        {
            return get(convNormToModel(i, offset, inverse));
        }

        public float getLast()
        {
            return get(Count - 1);
        }

        public void set(int i, float con)
        {
            if (i >= 0 && i < Count)
            {
                this[i] = con;
            }
        }

        public void setNorm(int i, int offset, bool inverse, float con)
        {
            set(convNormToModel(i, offset, inverse), con);
        }

        public float getNormCon(int i, int offset, bool inverse)
        {
            return getNorm(i, offset, inverse) / (inParams.vc / inParams.column);
        }

        public float getLastCol()
        {
            int pos = inParams.column2 - 1;
            if (pos >= Count)
            {
                pos = Count - 1;
            }
            return get(pos);
        }

        public void insertStartCol(float con)
        {
            this.Insert(0, con);
        }

        public void insertAfterCol(float con)
        {
            int insertpos = inParams.column2;
            if (insertpos >= Count)
            {
                insertpos = Count - 1;
            }
            Insert(insertpos, con);
        }

        public int convNormToModel(int i, int offset, bool inverse)
        {
			if (inverse)
			{
				return offset - 1 - i;
			}
            return i - offset;
        }

        public int convModelToNorm(int i, int offset, bool inverse)
        {
			if (inverse)
			{
				return offset - 1 - i;
			}
            return i + offset;
        }

    }
}

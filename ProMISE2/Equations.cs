using System;

namespace ProMISE2
{
	class Equations
	{
		public static float calcTransferU(KdefType kdef, float kx)
		{
            if (kdef == KdefType.U_L)
            {
                if (float.IsInfinity(kx))
                {
                    return 1;
                }
                else
                {
                    return kx / (kx + 1);
                }
            }
            else
            {
                if (float.IsInfinity(kx))
                {
                    return 0;
                }
                else
                {
                    return 1 / (kx + 1);
                }
            }
		}

		public static float calcTransferL(KdefType kdef, float kx)
		{
            if (kdef == KdefType.U_L)
            {
                if (float.IsInfinity(kx))
                {
                    return 0;
                }
                else
                {
                    return 1 / (kx + 1);
                }
            }
            else
            {
                if (float.IsInfinity(kx))
                {
                    return 1;
                }
                else
                {
                    return kx / (kx + 1);
                }
            }
		}

		public static float calcDc(KdefType kdef, float cu, float cl, float k)
		{
            if (kdef == KdefType.U_L)
            {
                return k * cl - cu;
            }
            else
            {
                return cl - k * cu;
            }
		}

		public static float calcPX(KdefType kdef, float lf, float uf)
		{
            if (kdef == KdefType.U_L)
            {
                return uf / lf;
            }
            else
            {
                return lf / uf;
            }
		}

		public static float calcElutable(KdefType kdef, float fu, float fl, float k)
		{
			// independent of magnitude of flow rate(s)
			return Math.Abs(calcFlow(kdef, fu, fl, k)) / (Math.Abs(fu) + Math.Abs(fl));
		}

		public static float calcFlow(KdefType kdef, float fu, float fl, float k)
		{
			// [vol/time]
            if (kdef == KdefType.U_L)
            {
                return fl + k * fu;
            }
            else
            {
                return fu + k * fl;
            }
		}

		public static float calcStepFlow(KdefType kdef, float lf, float uf, float fu, float fl, float k)
		{
			// [-]
            if (kdef == KdefType.U_L)
            {
                return lf * fl + k * uf * fu;
            }
            else
            {
                return uf * fu + k * lf * fl;
            }
		}

		public static float calcPos(KdefType kdef, float vc, float lf, float uf, float flow, float k)
		{
			// [vol] -> retention pos [time]
            if (kdef == KdefType.U_L)
            {
                return vc * (lf + k * uf) / flow;
            }
            else
            {
                return vc * (uf + k * lf) / flow;
            }
		}

		public static float calcInfPos(KdefType kdef, float vc, float lf, float uf, float fu, float fl)
		{
			// [vol] -> retention pos [time]
            if (kdef == KdefType.U_L)
            {
                return vc * uf / fu;
            }
            else
            {
                return vc * lf / fl;
            }
		}

		public static float calcColPos(KdefType kdef, float swtch, float lf, float uf, float flow, float k)
		{
			// [time] -> [vol]
            if (kdef == KdefType.U_L)
            {
                return swtch * flow / (lf + k * uf);
            }
            else
            {
                return swtch * flow / (uf + k * lf);
            }
		}

		public static float calcK(KdefType kdef, float vu, float vl, float fu, float fl, float t)
		{
			// [time] -> [k]
            if (kdef == KdefType.U_L)
            {
                return (t * fl - vl) / (vu - t * fu);
            }
            else
            {
                return (t * fu - vu) / (vl - t * fl);
            }
		}

		public static float calcEff1(float eff)
		{
			return eff / (float)Math.Pow(2, 1 - eff);
		}

		public static float calcSigma(KdefType kdef, float fnormu, float fnorml, float kx, float post, float rotspeed, float eff)
		{
			// [time] or [steps]
			float sigma;
			float dd;

			if (rotspeed == 0 || eff == 0)
				return 0;

            if (kdef == KdefType.U_L)
            {
                dd = fnorml + kx * fnormu;
            }
            else
            {
                dd = fnormu + kx * fnorml;
            }
			sigma = (float)Math.Sqrt(Math.Abs(post) * kx / rotspeed / eff);
            if (dd != 0 && !float.IsInfinity(dd))
            {
                sigma /= Math.Abs(dd);
            }
			return sigma;
		}

		public static float calcNSigma(KdefType kdef, float fnormu, float fnorml, float kx, float pos)
		{
			float sigma;
			float dd;

            if (kdef == KdefType.U_L)
            {
                dd = fnorml + kx * fnormu;
            }
            else
            {
                dd = fnormu + kx * fnorml;
            }
			sigma = (float)Math.Sqrt(Math.Abs(pos) * kx);
            if (dd != 0 && !float.IsInfinity(dd))
            {
                sigma /= Math.Abs(dd);
            }
			return sigma;
		}

		public static float calcColSigma(float sigma0, float ret, float colpos, float vc)
		{
			// [vol] or [steps]
			float sigma;

			sigma = sigma0 * (float)Math.Sqrt(Math.Abs(colpos) / vc) * vc / Math.Abs(ret);

			return sigma;
		}

		public static float calcHeight(float sigma)
		{
			return 1 / (sigma * (float)Math.Sqrt(2 * Math.PI));
		}

		public static float calcPlates(float pos, float sigma)
		{
			return (float)Math.Pow(pos / sigma, 2);
		}

		public static float calcRes(OutComp comp1, OutComp comp2)
		{
			float rs = 0;
			float dt;

			if (comp1.outCol && comp2.outCol)
			{
				if (comp1.phase == comp2.phase)
				{
					dt = Math.Abs(comp1.retention - comp2.retention);
				}
				else
				{
					dt = Math.Abs(comp1.retention) + Math.Abs(comp2.retention);
				}
				rs = dt / (comp1.width / 2 + comp2.width / 2);
			}
			return rs;
		}

		public static float calcBestWidth(float width1, float width2)
		{
			float dif;

			if (float.IsInfinity(width1) || width1 == 0)
			{
				// width1 useless
				return width2;
			}
			else if (float.IsInfinity(width2) || width2 == 0)
			{
				// width2 useless
				return width1;
			}

			dif = Math.Abs(width1 - width2) / ((width1 + width2) / 2);

			if (dif > 0.1)
			{
				// return smallest
				if (width1 < width2)
				{
					return width1;
				}
				else
				{
					return width2;
				}
			}
			return (width1 + width2) / 2; // average
		}

	}
}

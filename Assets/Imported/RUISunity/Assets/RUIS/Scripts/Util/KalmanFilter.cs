/*****************************************************************************

Content    :   Class for simple Kalman filtering
Authors    :   Tuukka Takala, derived from Peter Abeles' EJML Kalman class
Copyright  :   Copyright 2013 Tuukka Takala. All Rights reserved.
Licensing  :   RUIS is distributed under the LGPL Version 3 license.

******************************************************************************/

using UnityEngine;
using System;
using CSML;

/**
 * A Kalman filter is implemented by calling the generalized operations.  Much of the excessive
 * memory creation/destruction has been reduced from the KalmanFilterSimple.  However, there
 * is still room for improvement by using specialized algorithms directly.  The price paid
 * for this better performance is the need to manually manage memory and the need to have a
 * better understanding for how each of the operations works.
 *
 * <p>
 * This is an interface for a discrete time Kalman filter with no control input:<br>
 * <br>
 * x<sub>k</sub> = F<sub>k</sub> x<sub>k-1</sub> + w<sub>k</sub><br>
 * z<sub>k</sub> = H<sub>k</sub> x<sub>k</sub> + v<sub>k</sub> <br>
 * <br>
 * w<sub>k</sub> ~ N(0,Q<sub>k</sub>)<br>
 * v<sub>k</sub> ~ N(0,R<sub>k</sub>)<br>
 * </p>
 * @author Peter Abeles
 * 
 * ported to C# by Tuukka Takala
 */
public class KalmanFilter
{
    // kinematics description
    private Matrix F;
    private Matrix Q;
    private Matrix H;
    private Matrix R;

    // sytem state estimate
    private Matrix x;
    private Matrix P;

    // these are predeclared for efficency reasons
    private Matrix y,u,S,S_inv;
    private Matrix K;
	
	private Double[] state;
	
	private double[] previousMeasurements;
	private double[] tempZ;

	public bool skipIdenticalMeasurements = false;
	public int identicalMeasurementsCap = 10;
	private int identicalMeasurementsCount = 0;

    /**
     * Specify the kinematics model of the Kalman filter with  
     * default identity matrix values. This must be called 
     * first before any other functions.
     */
    public void initialize(int dimenX, int dimenZ)
    {
      F = Matrix.Identity(dimenX);
      Q = Matrix.Identity(dimenX);
      H = Matrix.Identity(dimenZ);
      R = new Matrix(dimenZ,dimenZ);

      y = new Matrix(dimenZ,1);
      u = new Matrix(dimenZ,1);
      S = new Matrix(dimenZ,dimenZ);
      S_inv = new Matrix(dimenZ,dimenZ);
      K = new Matrix(dimenX,dimenZ);

      x = new Matrix(dimenX,1);
      P = new Matrix(dimenX,dimenX);
      
      P = Matrix.Identity(dimenX);
		
      //x.zero(); // Should be zero already
	  state = new Double[dimenX];
	  for(int i = 0; i<state.Length; ++i)
	  		state[i] = 0;
		
	  previousMeasurements = new double[dimenZ];
	  for(int i = 0; i<previousMeasurements.Length; ++i)
	  		previousMeasurements[i] = 0;
    }

    /**
     * Specify the kinematics model of the Kalman filter.  This must be called
     * first before any other functions.
     *
     * @param F State transition matrix.
     * @param Q process noise covariance.
     * @param H measurement projection matrix.
     */
    public void initialize(Matrix F, Matrix Q, Matrix H) 
    {
	    int dimenX = F.ColumnCount;
	    int dimenZ = H.RowCount;

    	initialize(dimenX, dimenZ);
    		
        this.F = F;
        this.Q = Q;
        this.H = H;
    }

	public void reset()
	{
		initialize(state.Length, y.RowCount);
	}
	
    /**
     * The prior state estimate and covariance.
     *
     * @param x The estimated system state.
     * @param P The covariance of the estimated system state.
     */
    public void setState(Matrix x, Matrix P) 
    {
		this.x = x.Extract(1, x.RowCount, 1, x.ColumnCount); // this.x.set(x);
		this.P = P.Extract(1, P.RowCount, 1, P.ColumnCount); // this.P.set(P);
    }

    public void setQ(Matrix Q) 
    {
    	this.Q = Q.Extract(1, Q.RowCount, 1, Q.ColumnCount); // this.Q = Q;
    }

    public void setQ(double q) 
    {
    	Q = Matrix.Identity(Q.RowCount);
		Q = Q * q; // scale(q, Q);
    }

    public void setR(Matrix R) 
    {
    	this.R = R.Extract(1, R.RowCount, 1, R.ColumnCount); // this.S = R;
    }

    public void setR(double r) 
    {
    	R = Matrix.Identity(R.RowCount);
		R = R * r; //scale(r, S);
    }

    public void setR(int row, int col, double r) 
    {
		R[row, col] = new Complex(r);
    	//this.S.set(row, col, r);
    }
    
    /**
     * Predicts the state of the system forward one time step.
     */
    public void predict() {

        // x = F x
		x = F*x;
		x = x.Re();
        //mult(F,x,a);
        //x.set(a);

        // P = F P F' + Q
		
        P = ((F*P)*F.Transpose()) + Q;
		P = P.Re();
		//mult(F,P,b);
        //multTransB(b,F, P);
        //addEquals(P,Q);
    }

    /**
     * Updates the state provided the observation from a sensor.
     *
     * @param z Measurement.
     * @param R Measurement covariance.
     */
    public void update(Matrix z, Matrix R) 
	{
		if(skipIdenticalMeasurements)
		{
			bool areIdentical = true;
			
			for(int i = 0; i<z.RowCount; ++i)
			{
				if(z[i+1, 1].Re != previousMeasurements[i])
				{
					areIdentical = false;
					previousMeasurements[i] = z[i+1, 1].Re;
				}
			}
			
			if(areIdentical && identicalMeasurementsCount < identicalMeasurementsCap)
			{
				++identicalMeasurementsCount;
				return;
			}
			else
				identicalMeasurementsCount = 0;
		}
		
        // y = z - H x
        //mult(H,x,y);
        //sub(z,y,y);
		y = z - (H*x);
		y = y.Re();

        // S = H P H' + R
        //mult(H,P,c);
        //multTransB(c,H,S);
        //addEquals(S,R);
		S = ((H*P)*H.Transpose()) + R;
		S = S.Re();

        // K = PH'S^(-1)
        //if( !invert(S,S_inv) ) throw new RuntimeException("Invert failed");
        //multTransA(H,S_inv,d);
        //mult(P,d,K);
		try
		{
			S_inv = S.Inverse();
		}
		catch(Exception e)
		{
			Debug.Log("KalmanFilter.cs: Failed to invert S matrix.\n" + e);
		}
		K = P*(H.Transpose()*S_inv);
		K = K.Re();
			
        // x = x + Ky
        //mult(K,y,a);
        //addEquals(x,a);
		x = x + (K*y);

        // P = (I-kH)P = P - (KH)P = P-K(HP)
        //mult(H,P,c);
        //mult(K,c,b);
        //subEquals(P,b);
		P = P - (K*(H*P));
		P = P.Re();
    }


    /**
     * Updates the state provided the observation from a sensor.
     *
     * @param z Measurement.
     */
    public void update(double[] z) 
    {
		
		if(skipIdenticalMeasurements)
		{
			bool areIdentical = true;
			
			for(int i = 0; i<z.Length; ++i)
			{
				if(z[i] != previousMeasurements[i])
				{
					areIdentical = false;
					previousMeasurements[i] = z[i];
				}
			}
			
			if(areIdentical && identicalMeasurementsCount < identicalMeasurementsCap)
			{
				++identicalMeasurementsCount;
				return;
			}
			else
				identicalMeasurementsCount = 0;
		}
        // y = z - H x
        //mult(H,x,y);
        //for(int i=0; i<u.numRows; ++i)
        //	u.set(i, (double) z[i]);
        //sub(u,y,y);
		u = new Matrix(z);
		y = u - (H*x);
		y = y.Re();

        // S = H P H' + R
        //S_inv=S.copy();
        //mult(H,P,c);
        //multTransB(c,H,S);
        //addEquals(S,S_inv);
		S = ((H*P)*H.Transpose()) + R;
		S = S.Re();

        // K = PH'S^(-1)
        //if( !invert(S,S_inv) ) throw new RuntimeException("Invert failed");
        //multTransA(H,S_inv,d);
        //mult(P,d,K);
		try
		{
			S_inv = S.Inverse();
		}
		catch(Exception e)
		{
			Debug.Log("KalmanFilter.cs: Failed to invert S matrix.\n" + e);
		}
		K = P*(H.Transpose()*S_inv);
		K = K.Re();

        // x = x + Ky
        //mult(K,y,a);
        //addEquals(x,a);
		x = x + (K*y);

        // P = (I-kH)P = P - (KH)P = P-K(HP)
        //mult(H,P,c);
        //mult(K,c,b);
        //subEquals(P,b);
		P = P - (K*(H*P));
		P = P.Re();
    }
    
    /**
     * Returns the current estimated state of the system.
     *
     * @return The state.
     */
    public Double[] getState() 
    {
		for(int i = 0; i<state.Length; ++i)
	  		state[i] = x[i+1, 1].Re;
        return state;
    }

    /**
     * Returns the estimated state's covariance matrix.
     *
     * @return The covariance.
     */
    public Matrix getCovariance() 
    {
        return P;
    }
}
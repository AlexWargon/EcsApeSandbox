﻿
using System;
using System.Runtime.CompilerServices;

public static class Easings {
    /// <summary>
	/// Constant Pi.
	/// </summary>
	private const double PI = Math.PI; 
	
	/// <summary>
	/// Constant Pi / 2.
	/// </summary>
	private const double HALFPI = Math.PI / 2.0f; 
	
	/// <summary>
	/// Easing Functions enumeration
	/// </summary>
	public enum EasingType
	{
		Linear,
		QuadraticEaseIn,
		QuadraticEaseOut,
		QuadraticEaseInOut,
		CubicEaseIn,
		CubicEaseOut,
		CubicEaseInOut,
		QuarticEaseIn,
		QuarticEaseOut,
		QuarticEaseInOut,
		QuinticEaseIn,
		QuinticEaseOut,
		QuinticEaseInOut,
		SineEaseIn,
		SineEaseOut,
		SineEaseInOut,
		CircularEaseIn,
		CircularEaseOut,
		CircularEaseInOut,
		ExponentialEaseIn,
		ExponentialEaseOut,
		ExponentialEaseInOut,
		ElasticEaseIn,
		ElasticEaseOut,
		ElasticEaseInOut,
		BackEaseIn,
		BackEaseOut,
		BackEaseInOut,
		BounceEaseIn,
		BounceEaseOut,
		BounceEaseInOut
	}
	
	/// <summary>
	/// Interpolate using the specified function.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static public float Interpolate(float p, EasingType easingType)
	{
		switch(easingType)
		{
			default:
			case EasingType.Linear: 					return Linear(p);
			case EasingType.QuadraticEaseOut:		return QuadraticEaseOut(p);
			case EasingType.QuadraticEaseIn:			return QuadraticEaseIn(p);
			case EasingType.QuadraticEaseInOut:		return QuadraticEaseInOut(p);
			case EasingType.CubicEaseIn:				return CubicEaseIn(p);
			case EasingType.CubicEaseOut:			return CubicEaseOut(p);
			case EasingType.CubicEaseInOut:			return CubicEaseInOut(p);
			case EasingType.QuarticEaseIn:			return QuarticEaseIn(p);
			case EasingType.QuarticEaseOut:			return QuarticEaseOut(p);
			case EasingType.QuarticEaseInOut:		return QuarticEaseInOut(p);
			case EasingType.QuinticEaseIn:			return QuinticEaseIn(p);
			case EasingType.QuinticEaseOut:			return QuinticEaseOut(p);
			case EasingType.QuinticEaseInOut:		return QuinticEaseInOut(p);
			case EasingType.SineEaseIn:				return (float)SineEaseIn(p);
			case EasingType.SineEaseOut:				return (float)SineEaseOut(p);
			case EasingType.SineEaseInOut:			return (float)SineEaseInOut(p);
			case EasingType.CircularEaseIn:			return (float)CircularEaseIn(p);
			case EasingType.CircularEaseOut:			return (float)CircularEaseOut(p);
			case EasingType.CircularEaseInOut:		return (float)CircularEaseInOut(p);
			case EasingType.ExponentialEaseIn:		return (float)ExponentialEaseIn(p);
			case EasingType.ExponentialEaseOut:		return (float)ExponentialEaseOut(p);
			case EasingType.ExponentialEaseInOut:	return (float)ExponentialEaseInOut(p);
			case EasingType.ElasticEaseIn:			return (float)ElasticEaseIn(p);
			case EasingType.ElasticEaseOut:			return (float)ElasticEaseOut(p);
			case EasingType.ElasticEaseInOut:		return (float)ElasticEaseInOut(p);
			case EasingType.BackEaseIn:				return (float)BackEaseIn(p);
			case EasingType.BackEaseOut:				return (float)BackEaseOut(p);
			case EasingType.BackEaseInOut:			return (float)BackEaseInOut(p);
			case EasingType.BounceEaseIn:			return BounceEaseIn(p);
			case EasingType.BounceEaseOut:			return BounceEaseOut(p);
			case EasingType.BounceEaseInOut:			return BounceEaseInOut(p);
		}
	}
	
	/// <summary>
	/// Modeled after the line y = x
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static public float Linear(float p)
	{
		return p;
	}
	
	/// <summary>
	/// Modeled after the parabola y = x^2
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static public float QuadraticEaseIn(float p)
	{
		return p * p;
	}
	
	/// <summary>
	/// Modeled after the parabola y = -x^2 + 2x
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static public float QuadraticEaseOut(float p)
	{
		return -(p * (p - 2));
	}
	
	/// <summary>
	/// Modeled after the piecewise quadratic
	/// y = (1/2)((2x)^2)             ; [0, 0.5)
	/// y = -(1/2)((2x-1)*(2x-3) - 1) ; [0.5, 1]
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static public float QuadraticEaseInOut(float p)
	{
		if(p < 0.5f)
		{
			return 2 * p * p;
		}
		else
		{
			return (-2 * p * p) + (4 * p) - 1;
		}
	}
	
	/// <summary>
	/// Modeled after the cubic y = x^3
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static public float CubicEaseIn(float p)
	{
		return p * p * p;
	}
	
	/// <summary>
	/// Modeled after the cubic y = (x - 1)^3 + 1
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static public float CubicEaseOut(float p)
	{
		float f = (p - 1);
		return f * f * f + 1;
	}
	
	/// <summary>	
	/// Modeled after the piecewise cubic
	/// y = (1/2)((2x)^3)       ; [0, 0.5)
	/// y = (1/2)((2x-2)^3 + 2) ; [0.5, 1]
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static public float CubicEaseInOut(float p)
	{
		if(p < 0.5f)
		{
			return 4 * p * p * p;
		}
		else
		{
			float f = ((2 * p) - 2);
			return 0.5f * f * f * f + 1;
		}
	}
	
	/// <summary>
	/// Modeled after the quartic x^4
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static public float QuarticEaseIn(float p)
	{
		return p * p * p * p;
	}
	
	/// <summary>
	/// Modeled after the quartic y = 1 - (x - 1)^4
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static public float QuarticEaseOut(float p)
	{
		float f = (p - 1);
		return f * f * f * (1 - p) + 1;
	}
	
	/// <summary>
	// Modeled after the piecewise quartic
	// y = (1/2)((2x)^4)        ; [0, 0.5)
	// y = -(1/2)((2x-2)^4 - 2) ; [0.5, 1]
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static public float QuarticEaseInOut(float p) 
	{
		if(p < 0.5f)
		{
			return 8 * p * p * p * p;
		}
		else
		{
			float f = (p - 1);
			return -8 * f * f * f * f + 1;
		}
	}
	
	/// <summary>
	/// Modeled after the quintic y = x^5
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static public float QuinticEaseIn(float p) 
	{
		return p * p * p * p * p;
	}
	
	/// <summary>
	/// Modeled after the quintic y = (x - 1)^5 + 1
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static public float QuinticEaseOut(float p) 
	{
		float f = (p - 1);
		return f * f * f * f * f + 1;
	}
	
	/// <summary>
	/// Modeled after the piecewise quintic
	/// y = (1/2)((2x)^5)       ; [0, 0.5)
	/// y = (1/2)((2x-2)^5 + 2) ; [0.5, 1]
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static public float QuinticEaseInOut(float p) 
	{
		if(p < 0.5f)
		{
			return 16 * p * p * p * p * p;
		}
		else
		{
			float f = ((2 * p) - 2);
			return 0.5f * f * f * f * f * f + 1;
		}
	}
	
	/// <summary>
	/// Modeled after quarter-cycle of sine wave
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static public double SineEaseIn(float p)
	{
		return Math.Sin((p - 1) * HALFPI) + 1;
	}
	
	/// <summary>
	/// Modeled after quarter-cycle of sine wave (different phase)
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static public double SineEaseOut(float p)
	{
		return Math.Sin(p * HALFPI);
	}
	
	/// <summary>
	/// Modeled after half sine wave
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static public double SineEaseInOut(float p)
	{
		return 0.5f * (1 - Math.Cos(p * PI));
	}
	
	/// <summary>
	/// Modeled after shifted quadrant IV of unit circle
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static public double CircularEaseIn(float p)
	{
		return 1 - Math.Sqrt(1 - (p * p));
	}
	
	/// <summary>
	/// Modeled after shifted quadrant II of unit circle
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static public double CircularEaseOut(float p)
	{
		return Math.Sqrt((2 - p) * p);
	}
	
	/// <summary>	
	/// Modeled after the piecewise circular function
	/// y = (1/2)(1 - Math.Sqrt(1 - 4x^2))           ; [0, 0.5)
	/// y = (1/2)(Math.Sqrt(-(2x - 3)*(2x - 1)) + 1) ; [0.5, 1]
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static public double CircularEaseInOut(float p)
	{
		if(p < 0.5f)
		{
			return 0.5f * (1 - Math.Sqrt(1 - 4 * (p * p)));
		}
		else
		{
			return 0.5f * (Math.Sqrt(-((2 * p) - 3) * ((2 * p) - 1)) + 1);
		}
	}
	
	/// <summary>
	/// Modeled after the exponential function y = 2^(10(x - 1))
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static public double ExponentialEaseIn(float p)
	{
		return (p == 0.0f) ? p : Math.Pow(2, 10 * (p - 1));
	}
	
	/// <summary>
	/// Modeled after the exponential function y = -2^(-10x) + 1
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static public double ExponentialEaseOut(float p)
	{
		return (p == 1.0f) ? p : 1 - Math.Pow(2, -10 * p);
	}
	
	/// <summary>
	/// Modeled after the piecewise exponential
	/// y = (1/2)2^(10(2x - 1))         ; [0,0.5)
	/// y = -(1/2)*2^(-10(2x - 1))) + 1 ; [0.5,1]
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static public double ExponentialEaseInOut(float p)
	{
		if(p == 0.0 || p == 1.0) return p;
		
		if(p < 0.5f)
		{
			return 0.5f * Math.Pow(2, (20 * p) - 10);
		}
		else
		{
			return -0.5f * Math.Pow(2, (-20 * p) + 10) + 1;
		}
	}
	
	/// <summary>
	/// Modeled after the damped sine wave y = sin(13pi/2*x)*Math.Pow(2, 10 * (x - 1))
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static public double ElasticEaseIn(float p)
	{
		return Math.Sin(13 * HALFPI * p) * Math.Pow(2, 10 * (p - 1));
	}
	
	/// <summary>
	/// Modeled after the damped sine wave y = sin(-13pi/2*(x + 1))*Math.Pow(2, -10x) + 1
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static public double ElasticEaseOut(float p)
	{
		return Math.Sin(-13 * HALFPI * (p + 1)) * Math.Pow(2, -10 * p) + 1;
	}
	
	/// <summary>
	/// Modeled after the piecewise exponentially-damped sine wave:
	/// y = (1/2)*sin(13pi/2*(2*x))*Math.Pow(2, 10 * ((2*x) - 1))      ; [0,0.5)
	/// y = (1/2)*(sin(-13pi/2*((2x-1)+1))*Math.Pow(2,-10(2*x-1)) + 2) ; [0.5, 1]
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static public double ElasticEaseInOut(float p)
	{
		if(p < 0.5f)
		{
			return 0.5f * Math.Sin(13 * HALFPI * (2 * p)) * Math.Pow(2, 10 * ((2 * p) - 1));
		}
		else
		{
			return 0.5f * (Math.Sin(-13 * HALFPI * ((2 * p - 1) + 1)) * Math.Pow(2, -10 * (2 * p - 1)) + 2);
		}
	}
	
	/// <summary>
	/// Modeled after the overshooting cubic y = x^3-x*sin(x*pi)
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static public double BackEaseIn(float p)
	{
		return p * p * p - p * Math.Sin(p * PI);
	}
	
	/// <summary>
	/// Modeled after overshooting cubic y = 1-((1-x)^3-(1-x)*sin((1-x)*pi))
	/// </summary>	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static public double BackEaseOut(float p)
	{
		float f = (1 - p);
		return 1 - (f * f * f - f * Math.Sin(f * PI));
	}
	
	/// <summary>
	/// Modeled after the piecewise overshooting cubic function:
	/// y = (1/2)*((2x)^3-(2x)*sin(2*x*pi))           ; [0, 0.5)
	/// y = (1/2)*(1-((1-x)^3-(1-x)*sin((1-x)*pi))+1) ; [0.5, 1]
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static public double BackEaseInOut(float p)
	{
		if(p < 0.5f)
		{
			float f = 2 * p;
			return 0.5f * (f * f * f - f * Math.Sin(f * PI));
		}
		else
		{
			float f = (1 - (2*p - 1));
			return 0.5f * (1 - (f * f * f - f * Math.Sin(f * PI))) + 0.5f;
		}
	}
	
	/// <summary>
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static public float BounceEaseIn(float p)
	{
		return 1 - BounceEaseOut(1 - p);
	}
	
	/// <summary>
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static public float BounceEaseOut(float p)
	{
		if(p < 4/11.0f)
		{
			return (121 * p * p)/16.0f;
		}
		else if(p < 8/11.0f)
		{
			return (363/40.0f * p * p) - (99/10.0f * p) + 17/5.0f;
		}
		else if(p < 9/10.0f)
		{
			return (4356/361.0f * p * p) - (35442/1805.0f * p) + 16061/1805.0f;
		}
		else
		{
			return (54/5.0f * p * p) - (513/25.0f * p) + 268/25.0f;
		}
	}
	
	/// <summary>
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static float BounceEaseInOut(float p)
	{
		if(p < 0.5f)
		{
			return 0.5f * BounceEaseIn(p*2);
		}
		else
		{
			return 0.5f * BounceEaseOut(p * 2 - 1) + 0.5f;
		}
	}
}
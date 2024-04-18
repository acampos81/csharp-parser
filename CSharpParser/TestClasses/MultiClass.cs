using System;

namespace CSharpParser.TestClasses
{
  public abstract class Vehicle
  {
    protected int _wheels;
    protected float _weight;

    public Vehicle (int wheels, float weight)
    {
      _wheels = wheels;
      _weight = weight;
    }


    public string GetHornSound()
    {
      
      string hornSound = "Beep";

      if(_weight > 5000 || _wheels > 4)
      {
        hornSound = "HOOOOONK!";
      }

      return hornSound;
    }
  }


  public class Semi : Vehicle
  {
    public Semi() : base(18, 20000)
    {
      for(int i=0; i<_wheels; i++)
      {
        float wheelPressure = GetWheelPressure(i);

        if(wheelPressure < 55)
        {
          AddAir(i, 550);
        }
      }
    }

    private float GetWheelPressure(int tire)
    {
      // Wheel-pressure checking code goes here
      return 0f;
    }

    private void AddAir(int tire, float psi)
    {
      /* probably should add some safety code
       * here to prevent over inflation of tires */

      //TODO: Add tire code
      if(psi > 55)
      {
        Console.WriteLine("Whoa, it's going to pop!");
      }
    }
  }

  public class ElectricCar : Vehicle
  {
    bool _hasAutoPilot;
    int _price;

    public ElectricCar(int weight, int price) : base (4, weight)
    {
      _price = price;
      _hasAutoPilot = price > 500000;
    }

    public void Drive(float tempature)
    {
      if(tempature < 0)
      {
        for(int i=0; i<_price; i++)
        {
          Console.WriteLine("Oops, car can't start");
        }
      }
    }
  }
}

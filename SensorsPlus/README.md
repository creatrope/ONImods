How to use the Pressure Sensor Ribbon Outputs for PID Control in ONI:
•	Bit 0: Sensor's normal ON/OFF (threshold) output.
•	Bit 1: ON when the moving average of dP/dt > +threshold (pressure rising fast).
•	Bit 2: ON when the moving average of dP/dt < -threshold (pressure falling fast).
Example PID Logic (using standard ONI logic gates):
•	P (Proportional): Use Bit 0 directly for basic threshold control.
•	D (Derivative): Use Bit 1 (rising) and Bit 2 (falling) to detect rapid changes. Connect Bit 1 or Bit 2 to a NOT gate, then to an AND/OR gate with Bit 0 for more advanced control.
•	I (Integral): Use a Memory or Buffer gate to accumulate ON time from Bit 0.
Wiring Example:
•	Connect Bit 0 to a NOT gate, then to an AND gate with Bit 1 for a 'pressure rising above threshold' output.
•	Use OR gates to combine Bit 0 and Bit 1 for more responsive control.

Current Works with Atmo (Gas) Sensors, Liquid Pressure Sensors, and Thermo (Temperature) Sensors.
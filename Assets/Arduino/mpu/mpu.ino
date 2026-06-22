#include <Adafruit_MPU6050.h>
#include <Adafruit_Sensor.h>
#include <Wire.h>
#include <BluetoothSerial.h>

// Hardware vars
Adafruit_MPU6050 mpu;
BluetoothSerial serialBT;

// Software vars
float alpha = 0.01;
uint32_t prevTime = 0;
float compX = 0;
float compY = 0;

// Timing
uint32_t lastTime = 0;
uint32_t interval = 20; // corresponds to 50 hz

void setup() {
  Serial.begin(115200);
  serialBT.begin("ESP-32 (left v2)");
  Wire.begin();

  if (!mpu.begin()) {
    Serial.println("Error finding mpu.");
    while(1);
  }

  mpu.setGyroRange(MPU6050_RANGE_500_DEG);
  mpu.setAccelerometerRange(MPU6050_RANGE_8_G);
  mpu.setFilterBandwidth(MPU6050_BAND_21_HZ);
}

void loop() {
  sensors_event_t accel, gyro, temp;
  mpu.getEvent(&accel, &gyro, &temp);

  uint32_t timestamp = millis();
  // only send data to Unity at predefined interval
  if (timestamp - lastTime < interval) {
    delay(1);
    return;
  }
  lastTime = timestamp;

  float dt = (timestamp - prevTime) / 1000.0; // convert from ms to s
  prevTime = timestamp;

  // Raw gyro data converted to degrees
  float rateRoll = (float)gyro.gyro.x * 180.0 / PI;
  float ratePitch = (float)gyro.gyro.y * 180.0 / PI;

  // Raw accel data in m/s^2
  float accX = (float)accel.acceleration.x;
  float accY = (float)accel.acceleration.y;
  float accZ = (float)accel.acceleration.z;

  // Rotation angles from accelerometer
  float angleRoll = atan(accY/sqrt(accX*accX+accZ*accZ))*1/(3.142/180);
  float anglePitch = atan(accX/sqrt(accY*accY+accZ*accZ))*1/(3.142/180);

  // Complementery filter application
  compX = alpha * (compX + rateRoll * dt) + (1.0 - alpha) * angleRoll;
  compY = alpha * (compY + ratePitch * dt) + (1.0 - alpha) * anglePitch;

  // Send data to Unity
  String payload = String(compX) + "," + String(compY);
  Serial.println(payload);

  serialBT.println(payload);
}

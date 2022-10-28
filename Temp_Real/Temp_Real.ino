#include <GyverMAX6675.h>

#define CLK_PIN   4
#define DATA_PIN  2
#define CS_PIN    3

String incomingString;
byte buffer[20];
int temp=0;
bool active=false;
GyverMAX6675<CLK_PIN, DATA_PIN, CS_PIN> sens;

void setup() {
  Serial.begin(9600);
  pinMode(5,OUTPUT);
}  
void loop() {
  if (Serial.available() > 0) {
    incomingString = Serial.readStringUntil('\n');
    incomingString.toCharArray(buffer, incomingString.length() + 1);
    
    if ((buffer[0] == 82)){    //R
      if((buffer[1]==83)){
        if(active)
          Serial.println("1");
        else
          Serial.println("0");
      }else
      {
        if (sens.readTemp()) {
          temp=sens.getTemp();
        }
        Serial.println(temp);
      }
    }else{
      if ((buffer[0] == 87) && (buffer[1] == 59)){    //W;
        if(buffer[2] == 49){
          Serial.println("S;1");
          digitalWrite(5, HIGH);
          active=true;
        }
        else{
          Serial.println("S;0");
          digitalWrite(5, LOW);
          active=false;
        }
      }
    }
  }

}

int temp_increment = 1;
int temp_decrement = 1;
int ambient_temp = 20; 

String incomingString;
byte buffer[20];
int temp=ambient_temp;
bool active=false;

unsigned long previousMillis = 0;
const long interval = 1000;

void setup() {
  Serial.begin(9600);
  pinMode(LED_BUILTIN,OUTPUT);
}  
void loop() {
  unsigned long currentMillis = millis();
  if (currentMillis - previousMillis >= interval) 
  {
    previousMillis = currentMillis;

    if (active) 
    {
      temp += temp_increment;
    } 
    else 
    {
      temp -= temp_decrement;
      if (temp < ambient_temp) 
      {
        temp = ambient_temp;
      }
    }
  }
  if (Serial.available() > 0) {
    incomingString = Serial.readStringUntil('\n');
    incomingString.toCharArray(buffer, incomingString.length() + 1);
    
    if ((buffer[0] == 82)){    //R
        if(active){
          Serial.print(temp);
          Serial.println(";1");
        }
        else{
          Serial.print(temp);
          Serial.println(";0");
        }
    }else{
      if ((buffer[0] == 87) && (buffer[1] == 59)){    //W;
        if(buffer[2] == 49){
          Serial.println("S;1");
          digitalWrite(LED_BUILTIN, HIGH);
          active=true;
        }
        else{
          Serial.println("S;0");
          digitalWrite(LED_BUILTIN, LOW);
          active=false;
        }
      }
    }
  }
}

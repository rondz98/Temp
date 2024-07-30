int temp_increment = 10;
int temp_decrement = 7;
int ambient_temp = 20; 

String incomingString;
byte buffer[20];
int temp=0;
bool active=false;

unsigned long previousMillis = 0;
const long interval = 1000;

void setup() {
  Serial.begin(9600);
  pinMode(5,OUTPUT);
}  
void loop() {
  if (Serial.available() > 0) {
    incomingString = Serial.readStringUntil('\n');
    incomingString.toCharArray(buffer, incomingString.length() + 1);
    
    if ((buffer[0] == 82)){    //R
        if(active)
          Serial.println(temp+";1");
        else
          Serial.println(temp+";0");
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
}

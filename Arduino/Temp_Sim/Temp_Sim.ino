String incomingString;
byte buffer[20];
int temp=20;
bool active=false;
const int hystvalue=50;
const int mantainvalue=3;
int i=0;
int im=mantainvalue;
void setup() {
  Serial.begin(9600);
  pinMode(5,OUTPUT);
}  
void loop() {
  if (Serial.available() > 0) {
    incomingString = Serial.readStringUntil('\n');
    incomingString.toCharArray(buffer, incomingString.length() + 1);
    
    if ((buffer[0] == 82)){    //R
      if(active==true){
        temp++;
      }else{
        if(i>0){
          temp++;
          i--;
        }else{
          if(im>0)
            im--;
          else{
            im=0;
            if(temp>20)
              temp--;
          }
        }
      }
      Serial.println(temp);
    }else{
      if ((buffer[0] == 87) && (buffer[1] == 59)){    //W;
        if(buffer[2] == 49){
          Serial.println("S;1");
          active=true;
          i=hystvalue;
          im=mantainvalue;
          digitalWrite(5, HIGH);
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

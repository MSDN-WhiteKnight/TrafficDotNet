// EtwNetworkTest.cpp: главный файл проекта.
#include <stdlib.h>
#include <locale.h>
#include <stdio.h>
using namespace EtwNetwork;




/* **** Client Part *** */


public ref class Foo{
public:	
	static int c = 0;
	static void EventHandler(System::Object^ sender, EtwEvent^ e)
	{
		System::Console::WriteLine();
		System::Console::WriteLine(e->ToString());
		System::Console::WriteLine();
		c++;
		if(c>20) {EtwSession::Stop();System::Console::WriteLine("Thanks, enough events.");}
	}
};



int main(array<System::String ^> ^args)
{
        

    setlocale(LC_ALL,"Russian");

	EtwSession::NewEvent += gcnew EventDelegate(Foo::EventHandler);
	EtwSession::Start();


    system("PAUSE");
    
    return 0;
}

// --------------------------------------------------
// a simple implementation of an equally simple stack
// --------------------------------------------------
#ifndef STACK
#define STACK
namespace U6Stack
{
   const int stack_size = 10000;

   unsigned char stack[stack_size];
   int contains;

   void init()
   {
      contains = 0;
   }

   bool is_empty()
   {
      return (contains==0);
   }

   bool is_full()
   {
      return(contains==stack_size);
   }

   void push(unsigned char element)
   {
      if (!is_full())
      {
         stack[contains] = element;
         contains++;   
      }
   }

   unsigned char pop()
   {
      unsigned char element;
      
      if (!is_empty())
      {
         element = stack[contains-1];
         contains--;
      }
      else
      {
         element = 0;
      }
      return(element);
   }

   unsigned char gettop()
   {
      if (!is_empty())
      {
         return(stack[contains-1]);
      }
   }
}


#endif
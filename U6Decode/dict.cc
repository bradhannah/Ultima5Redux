// --------------------------------------------------
// a dictionary
// --------------------------------------------------
#ifndef DICT
#define DICT
namespace U6Dict
{
   struct dict_entry
   {
      unsigned char root;
      int codeword;
   };

   const int dict_size = 10000;

   dict_entry dict[dict_size];
   int contains;

   void init()
   {
      contains = 0x102;
   }

   void add(unsigned char root, int codeword)
   {
      dict[contains].root = root;
      dict[contains].codeword = codeword;
      contains++;
   }

   unsigned char get_root(int codeword)
   {
      return (dict[codeword].root);
   }

   int get_codeword(int codeword)
   {
      return (dict[codeword].codeword);
   }

}


#endif
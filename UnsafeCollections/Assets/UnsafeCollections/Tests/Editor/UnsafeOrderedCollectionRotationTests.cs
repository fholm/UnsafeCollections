/*
The MIT License (MIT)

Copyright (c) 2019 Fredrik Holmstrom

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

using NUnit.Framework;

namespace Collections.Unsafe {
  // all test cases taken from the two top answers here= https=//stackoverflow.com/questions/3955680/howRtoRcheckRifRmyRavlRtreeRimplementationRisRcorrect

  public unsafe class UnsafeOrderedCollectionRotationTests {
    static UnsafeOrderedCollection* Tree(params int[] values) {
      var c = UnsafeOrderedCollection.Allocate<int>(values.Length * 2);

      for (int i = 0; i < values.Length; ++i) {
        UnsafeOrderedCollection.Insert(c, values[i]);
      }

      return c;
    }

    static void AssertTree(UnsafeOrderedCollection* c, string expected) {
      NUnit.Framework.Assert.AreEqual(expected, UnsafeOrderedCollection.PrintTree<int>(c, Print));
    }

    static string Print(int value) {
      return value.ToString();
    }

    // insert simple

    [Test]
    public void InsertSimpleLeftRotation() {
      var t = Tree(1, 2);
      AssertTree(t, "1R=*|2");
      UnsafeOrderedCollection.Insert(t, 3);
      AssertTree(t, "2=1|3");
      UnsafeOrderedCollection.Free(t);
    }

    [Test]
    public void InsertSimpleRightLeftRotation() {
      var t = Tree(3, 1);
      AssertTree(t, "3L=1|*");
      UnsafeOrderedCollection.Insert(t, 2);
      AssertTree(t, "2=1|3");
      UnsafeOrderedCollection.Free(t);
    }

    [Test]
    public void InsertSimpleRightRotation() {
      var t = Tree(3, 2);
      AssertTree(t, "3L=2|*");
      UnsafeOrderedCollection.Insert(t, 1);
      AssertTree(t, "2=1|3");
      UnsafeOrderedCollection.Free(t);
    }

    [Test]
    public void InsertSimpleLeftRightRotation() {
      var t = Tree(2, 3);
      AssertTree(t, "2R=*|3");
      UnsafeOrderedCollection.Insert(t, 1);
      AssertTree(t, "2=1|3");
      UnsafeOrderedCollection.Free(t);
    }

    // delete simple

    [Test]
    public void DeleteSimpleLeftRotation() {
      var t = Tree(2, 1, 3, 4);
      AssertTree(t, "2R=1|[3R=*|4]");
      UnsafeOrderedCollection.Remove(t, 1);
      AssertTree(t, "3=2|4");
      UnsafeOrderedCollection.Free(t);
    }

    [Test]
    public void DeleteSimpleRightLeftRotation() {
      var t = Tree(2, 1, 4, 3);
      AssertTree(t, "2R=1|[4L=3|*]");
      UnsafeOrderedCollection.Remove(t, 1);
      AssertTree(t, "3=2|4");
      UnsafeOrderedCollection.Free(t);
    }

    [Test]
    public void DeleteSimpleRightRotation() {
      var t = Tree(3, 2, 4, 1);
      AssertTree(t, "3L=[2L=1|*]|4");
      UnsafeOrderedCollection.Remove(t, 4);
      AssertTree(t, "2=1|3");
      UnsafeOrderedCollection.Free(t);
    }

    [Test]
    public void DeleteSimpleLeftRightRotation() {
      var t = Tree(3, 4, 1, 2);
      AssertTree(t, "3L=[1R=*|2]|4");
      UnsafeOrderedCollection.Remove(t, 4);
      AssertTree(t, "2=1|3");
      UnsafeOrderedCollection.Free(t);
    }

    // insert complex

    [Test]
    public void InsertComplexLeftRotation() {
      var t = Tree(3, 2, 5, 4, 6);
      AssertTree(t, "3R=2|[5=4|6]");
      UnsafeOrderedCollection.Insert(t, 7);
      AssertTree(t, "5=[3=2|4]|[6R=*|7]");
    }

    [Test]
    public void InsertComplexRightLeftRotation() {
      var t = Tree(5, 3, 10, 2, 4, 8, 11, 7, 9, 12);
      AssertTree(t, "5R=[3=2|4]|[10=[8=7|9]|[11R=*|12]]");
      UnsafeOrderedCollection.Insert(t, 6);
      AssertTree(t, "8=[5=[3=2|4]|[7L=6|*]]|[10R=9|[11R=*|12]]");
    }

    [Test]
    public void InsertComplexRightRotation() {
      var t = Tree(6, 7, 4, 5, 3);
      AssertTree(t, "6L=[4=3|5]|7");
      UnsafeOrderedCollection.Insert(t, 1);
      AssertTree(t, "4=[3L=1|*]|[6=5|7]");
    }

    [Test]
    public void InsertComplexLeftRightRotation() {
      var t = Tree(8, 3, 11, 2, 5, 9, 12, 1, 4, 6);
      AssertTree(t, "8L=[3=[2L=1|*]|[5=4|6]]|[11=9|12]");
      UnsafeOrderedCollection.Insert(t, 7);
      AssertTree(t, "5=[3L=[2L=1|*]|4]|[8=[6R=*|7]|[11=9|12]]");
    }

    // delete complex

    [Test]
    public void DeleteComplexLeftRotation() {
      var t = Tree(3, 2, 5, 1, 4, 6, 7);
      AssertTree(t, "3R=[2L=1|*]|[5R=4|[6R=*|7]]");
      UnsafeOrderedCollection.Remove(t, 1);
      AssertTree(t, "5=[3=2|4]|[6R=*|7]");
    }

    [Test]
    public void DeleteComplexRightLeftRotation() {
      var t = Tree(5, 3, 10, 1, 4, 8, 11, 2, 7, 9, 12, 6);
      AssertTree(t, "5R=[3L=[1R=*|2]|4]|[10L=[8L=[7L=6|*]|9]|[11R=*|12]]");
      UnsafeOrderedCollection.Remove(t, 2);
      AssertTree(t, "8=[5=[3=1|4]|[7L=6|*]]|[10R=9|[11R=*|12]]");
    }

    [Test]
    public void DeleteComplexRightRotation() {
      var t = Tree(5, 3, 7, 2, 4, 6, 1);
      AssertTree(t, "5L=[3L=[2L=1|*]|4]|[7L=6|*]");
      UnsafeOrderedCollection.Remove(t, 6);
      AssertTree(t, "3=[2L=1|*]|[5=4|7]");
    }

    [Test]
    public void DeleteComplexLeftRightRotation() {
      var t = Tree(8, 3, 11, 2, 5, 9, 12, 1, 4, 6, 10, 7);
      AssertTree(t, "8L=[3R=[2L=1|*]|[5R=4|[6R=*|7]]]|[11L=[9R=*|10]|12]");
      UnsafeOrderedCollection.Remove(t, 10);
      AssertTree(t, "5=[3L=[2L=1|*]|4]|[8=[6R=*|7]|[11=9|12]]");
    }

    // insert case1, case2 and case3

    static int[] _insertCase1 = new[] { 20, 4 };
    static int[] _insertCase2 = new[] { 20, 4, 26, 3, 9 };
    static int[] _insertCase3 = new[] { 20, 4, 26, 3, 9, 21, 30, 2, 7, 11 };

    [Test]
    public void InsertCase1() {
      UnsafeOrderedCollection* t;

      t = Tree(_insertCase1);
      AssertTree(t, "20L=4|*");
      UnsafeOrderedCollection.Insert(t, 15);
      AssertTree(t, "15=4|20");

      t = Tree(_insertCase1);
      UnsafeOrderedCollection.Insert(t, 8);
      AssertTree(t, "8=4|20");
    }

    [Test]
    public void InsertCase2() {
      UnsafeOrderedCollection* t;

      t = Tree(_insertCase2);
      AssertTree(t, "20L=[4=3|9]|26");
      UnsafeOrderedCollection.Insert(t, 15);
      AssertTree(t, "9=[4L=3|*]|[20=15|26]");

      t = Tree(_insertCase2);
      UnsafeOrderedCollection.Insert(t, 8);
      AssertTree(t, "9=[4=3|8]|[20R=*|26]");
    }

    [Test]
    public void InsertCase3() {
      UnsafeOrderedCollection* t;

      t = Tree(_insertCase3);
      AssertTree(t, "20L=[4=[3L=2|*]|[9=7|11]]|[26=21|30]");
      UnsafeOrderedCollection.Insert(t, 15);
      AssertTree(t, "9=[4L=[3L=2|*]|7]|[20=[11R=*|15]|[26=21|30]]");

      t = Tree(_insertCase3);
      AssertTree(t, "20L=[4=[3L=2|*]|[9=7|11]]|[26=21|30]");
      UnsafeOrderedCollection.Insert(t, 8);
      AssertTree(t, "9=[4=[3L=2|*]|[7R=*|8]]|[20R=11|[26=21|30]]");
    }

    // delete case1, case2 and case3

    static int[] _deleteCase1 = new[] { 2, 1, 4, 3, 5 };
    static int[] _deleteCase2 = new[] { 6, 2, 9, 1, 4, 8, 11, 3, 5, 7, 10, 12, 13 };
    static int[] _deleteCase3 = new[] { 5, 2, 8, 1, 3, 7, 10, 4, 6, 9, 11, 12 };

    [Test]
    public void DeleteCase1() {
      UnsafeOrderedCollection* t;

      t = Tree(_deleteCase1);
      AssertTree(t, "2R=1|[4=3|5]");
      UnsafeOrderedCollection.Remove(t, 1);
      AssertTree(t, "4L=[2R=*|3]|5");
    }

    [Test]
    public void DeleteCase2() {
      UnsafeOrderedCollection* t;

      t = Tree(_deleteCase2);
      AssertTree(t, "6R=[2R=1|[4=3|5]]|[9R=[8L=7|*]|[11R=10|[12R=*|13]]]");
      UnsafeOrderedCollection.Remove(t, 1);
      AssertTree(t, "6R=[4L=[2R=*|3]|5]|[9R=[8L=7|*]|[11R=10|[12R=*|13]]]");
    }

    [Test]
    public void DeleteCase3() {
      UnsafeOrderedCollection* t;

      t = Tree(_deleteCase3);
      AssertTree(t, "5R=[2R=1|[3R=*|4]]|[8R=[7L=6|*]|[10R=9|[11R=*|12]]]");
      UnsafeOrderedCollection.Remove(t, 1);
      AssertTree(t, "8=[5=[3=2|4]|[7L=6|*]]|[10R=9|[11R=*|12]]");
    }
  }
}
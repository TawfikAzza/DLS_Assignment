<script setup lang="ts">
import { ref } from 'vue';

const history = ref<any>([
  { id: 1, a: 1, b: 2, operand: '+', result: 3 },
  { id: 2, a: 3, b: 2, operand: '-', result: 1 },
  { id: 3, a: 4, b: 2, operand: '*', result: 8 },
  { id: 4, a: 6, b: 2, operand: '/', result: 3 },
]);

const a = ref<number | null>(null);
const b = ref<number | null>(null);
const operand = ref<string | null>(null);

const calculate = () => { //TODO: API call
  if (a.value === null || b.value === null) {
    return;
  }

  let result = 0;
  switch (operand.value) {
    case '+':
      result = a.value + b.value;
      break;
    case '-':
      result = a.value - b.value;
      break;
    case '*':
      result = a.value * b.value;
      break;
    case '/':
      result = a.value / b.value;
      break;
  }

  history.value.unshift({
    id: history.value.length + 1,
    a: a.value,
    b: b.value,
    operand: operand.value,
    result: result
  });

  a.value = null;
  b.value = null;
};

</script>

<template>
  <header>

  </header>

  <main>
    <h1>Calculator</h1>
    <div>
      <input type="number" v-model="a" />
      <select v-model="operand">
        <option value="+">+</option>
        <option value="-">-</option>
        <option value="*">*</option>
        <option value="/">/</option>
      </select>
      <input type="number" v-model="b" />
      <button @click="calculate">Calculate</button>
    </div>

    <h1>History</h1>
    <p v-for="item in history" :key="item.id">
      {{ item.a }} {{ item.operand }} {{ item.b }} = {{ item.result }}
    </p>
  </main>
</template>

<style scoped>

</style>

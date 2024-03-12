<script setup lang="ts">
import { ref } from 'vue';

const history = ref<any>([
  { a: 1, b: 2, operation: '+', result: 3 },
  { a: 3, b: 2, operation: '-', result: 1 },
  { a: 4, b: 2, operation: '*', result: 8 },
  { a: 6, b: 2, operation: '/', result: 3 },
]);

const a = ref<number | null>(null);
const b = ref<number | null>(null);
const operation = ref<string | null>(null);

const calculate = async () => { //TODO: API call
  if (a.value === null
      || b.value === null
      || operation.value === null) {
    return;
  }

  const problem = {
    operandA: a.value,
    operandB: b.value,
  };

  const request = {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(problem),
  };
  let requestUrlRoot = 'http://localhost/Main';
  if (operation.value === '+') {
    requestUrlRoot += '/Sum';
  } else if (operation.value === '-') {
    requestUrlRoot += '/Subtract';
  } else {
    console.error('Invalid operation"' + operation.value + '"');
    return;
  }

  //Get the result from the server
  const response = await fetch(requestUrlRoot, request);
  const result = await response.json();

  //Add the result to the history
  history.value
    .push({
      a: a.value,
      b: b.value,
      operation: operation.value,
      result: result,
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
      <select v-model="operation">
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
      {{ item.a }} {{ item.operation }} {{ item.b }} = {{ item.result }}
    </p>
  </main>
</template>

<style scoped>

</style>

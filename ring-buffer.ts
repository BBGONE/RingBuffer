    // Интерфейс реализуемый классом кольцевого буфера
    export interface IRingBuffer<T> {
        capacity: number;
        isEmpty: boolean;
        isFull: boolean;
        size: number;
        peek(): T;
        peekN(count: number): T[];
        enq(item: T): number;
        deq(): T | undefined;
        deqN(count: number): T[];
        clear(): void;
    }

    // Метод создания экземпляра буфера
    export function createRingBuffer<T>(capacity: number, evictedCb: (item: T) => void = null): IRingBuffer<T> {
        return new RingBuffer(capacity, evictedCb);
    }

    // Класс кольцевого буфера
    class RingBuffer<T = any> implements IRingBuffer<T> {

        //#region Поля
        private _first: number = 0;
        private _size: number = 0;
        private _items: T[] = [];
        private _evictedCb: (item: T) => void | null;
        private readonly _capacity: number;
        //#endregion

        //#region Конструктор
        constructor(capacity: number = 10, evictedCb: (item: T) => void = null) {
            this._capacity = capacity;
            this._evictedCb = evictedCb;
        }
        //#endregion

        //#region Свойства

        // вместимость буфера
        get capacity(): number {
            return this._capacity;
        }

        // пустой ли буфер
        get isEmpty(): boolean {
            return this._size === 0;
        }

        // полный ли буфер
        get isFull(): boolean {
            return this._size === this._capacity;
        }

        // текущее кол-во элементов в буфере
        get size(): number {
            return this._size;
        }

        //#endregion

        //#region Методы

        // Метод возвращает запрашиваемое число элементов с начала буфера (без удаления)
        private _peekN(count: number, clearReturned: boolean): T[] {
            if (count > this._size) {
                count = this._size;
            }

            if (count === 0) {
                return [];
            }

            const end1 = Math.min(this._first + count, this._capacity);
            const firstHalf = this._items.slice(this._first, end1);

            if (clearReturned) {
                for (let i = this._first; i < end1; ++i) {
                    this._items[i] = undefined;
                }
            }

            if (end1 < this._capacity) {
                return firstHalf;
            }
            const end2 = count - firstHalf.length;
            const secondHalf = this._items.slice(0, end2);

            if (clearReturned) {
                for (let i = 0; i < end2; ++i) {
                    this._items[i] = undefined;
                }
            }

            return firstHalf.concat(secondHalf);
        }

        // Метод возвращает элемент с начала буфера (без удаления)
        public peek(): T {
            if (this.isEmpty) {
                return undefined;
            }

            return this._items[this._first];
        }

        // Метод возвращает запрашиваемое число элементов с начала буфера (без удаления)
        public peekN(count: number): T[] {
            return this._peekN(count, false);
        }

        // Метод получения элемента из буфера
        public deq(): T | undefined {
            const res = this.peek();

            if (res !== undefined) {
                this._items[this._first] = undefined;
                this._size--;
                this._first = (this._first + 1) % this._capacity;
                return res;
            }

            return res;
        }

        // Метод получения элементов из буфера
        public deqN(count: number): T[] {
            const res = this._peekN(count, true);
            count = res.length;
            this._size -= count;
            this._first = (this._first + count) % this._capacity;

            return res;
        }

        // Метод помещения элемента в буфер
        // возвращает текущий размер буфера
        public enq(item: T): number {
            const end = (this._first + this._size) % this._capacity;
            const full = this.isFull;

            if (full && this._evictedCb) {
                this._evictedCb(this._items[end]);
            }
            this._items[end] = item;

            if (full) {
                this._first = (this._first + 1) % this._capacity;
            } else {
                this._size++;
            }

            return this._size;
        }


        // Метод удаления всех элементов из буфера
        public clear(): void {
            this._items = [];
            this._first = 0;
            this._size = 0;
        }

        //#endregion
    }

